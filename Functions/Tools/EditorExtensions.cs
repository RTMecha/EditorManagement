using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

using BeatmapObject = DataManager.GameData.BeatmapObject;

namespace EditorManagement.Functions.Tools
{
    public static class EditorExtensions
    {
		public static List<GameObject> Range(Color _min, Color _max)
        {
			List<GameObject> a = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
							  where x.GetComponent<Image>() && x.GetComponent<Image>().color.r < _max.r && x.GetComponent<Image>().color.g < _max.g && x.GetComponent<Image>().color.b < _max.b && x.GetComponent<Image>().color.a < _max.a && x.GetComponent<Image>().color.r > _min.r && x.GetComponent<Image>().color.g > _min.g && x.GetComponent<Image>().color.b > _min.b && x.GetComponent<Image>().color.a > _min.a
							select x).ToList();
			return a;
		}

		//EditorExtensions.ColorRange(new Color(0.1294f, 0.1294f, 0.1294f, 1f), 0.1f)
		public static List<GameObject> ColorRange(Color _base, float _range)
        {
			List<GameObject> a = new List<GameObject>();

			if (_range == 0f)
			{
				a = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
									  where x.GetComponent<Image>() && x.GetComponent<Image>().color.r == _base.r && x.GetComponent<Image>().color.g == _base.g && x.GetComponent<Image>().color.b == _base.b && x.GetComponent<Image>().color.a == _base.a || x.GetComponent<Text>() && x.GetComponent<Text>().color.r == _base.r && x.GetComponent<Text>().color.g == _base.g && x.GetComponent<Text>().color.b == _base.b && x.GetComponent<Text>().color.a == _base.a
					 select x).ToList();
			}
			else
            {
				a = (from x in Resources.FindObjectsOfTypeAll<GameObject>()
				 where x.GetComponent<Image>() && x.GetComponent<Image>().color.r < _base.r + _range && x.GetComponent<Image>().color.g < _base.g + _range && x.GetComponent<Image>().color.b < _base.b + _range && x.GetComponent<Image>().color.a < _base.a + _range && x.GetComponent<Image>().color.r > _base.r + -_range && x.GetComponent<Image>().color.g > _base.g + -_range && x.GetComponent<Image>().color.b > _base.b + -_range && x.GetComponent<Image>().color.a > _base.a + -_range || x.GetComponent<Text>() && x.GetComponent<Text>().color.r < _base.r + _range && x.GetComponent<Text>().color.g < _base.g + _range && x.GetComponent<Text>().color.b < _base.b + _range && x.GetComponent<Text>().color.a < _base.a + _range && x.GetComponent<Text>().color.r > _base.r + -_range && x.GetComponent<Text>().color.g > _base.g + -_range && x.GetComponent<Text>().color.b > _base.b + -_range && x.GetComponent<Text>().color.a > _base.a + -_range
				 select x).ToList();
			}
			return a;
		}

		public static GameObject GetGameObject(this BeatmapObject _beatmapObject)
		{
			if (EditorPlugin.catalyst != null && EditorPlugin.catInstalled == 3)
			{
				var iLevelObject = _beatmapObject.GetILevelObject();
				var visualObject = iLevelObject.GetType().GetField("visualObject", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(iLevelObject);

				return (GameObject)visualObject.GetType().GetField("gameObject", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(visualObject);
			}

			var chain = _beatmapObject.GetTransformChain();

			if (chain.Count < 1)
            {
				return null;
            }

			return chain[chain.Count - 1].gameObject;
		}

		public static List<BeatmapObject> GetParentChain(this BeatmapObject _beatmapObject)
		{
			List<BeatmapObject> beatmapObjects = new List<BeatmapObject>();

			if (_beatmapObject != null)
			{
				var orig = _beatmapObject;
				beatmapObjects.Add(orig);

				while (!string.IsNullOrEmpty(orig.parent))
				{
					if (orig == null || DataManager.inst.gameData.beatmapObjects.Find(x => x.id == orig.parent) == null)
						break;
					var select = DataManager.inst.gameData.beatmapObjects.Find(x => x.id == orig.parent);
					beatmapObjects.Add(select);
					orig = select;
				}
			}

			return beatmapObjects;
		}

		public static List<Transform> GetTransformChain(this BeatmapObject _beatmapObject)
		{
			var list = new List<Transform>();
			if (EditorPlugin.catalyst != null && EditorPlugin.catInstalled == 3)
			{
				var tf1 = _beatmapObject.GetGameObject().transform;

				while (tf1.parent != null && tf1.parent.gameObject.name != "GameObjects")
				{
					tf1 = tf1.parent;
				}

				list.Add(tf1);

				while (tf1.childCount != 0 && tf1.GetChild(0) != null)
				{
					tf1 = tf1.GetChild(0);
					list.Add(tf1);
				}

				return list;
			}

			if (ObjectManager.inst == null || ObjectManager.inst.beatmapGameObjects.Count < 1 || !ObjectManager.inst.beatmapGameObjects.ContainsKey(_beatmapObject.id))
            {
				return list;
            }

			var gameObjectRef = ObjectManager.inst.beatmapGameObjects[_beatmapObject.id];
			var tf = gameObjectRef.obj.transform;
			list.Add(tf);

			while (tf.childCount != 0 && tf.GetChild(0) != null)
			{
				tf = tf.GetChild(0);
				list.Add(tf);
			}

			return list;
		}

		public static List<List<BeatmapObject>> GetChildChain(this BeatmapObject _beatmapObject)
		{
			var lists = new List<List<BeatmapObject>>();
			foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
			{
				if (beatmapObject.GetParentChain() != null && beatmapObject.GetParentChain().Count > 0)
				{
					var parentChain = beatmapObject.GetParentChain();
					foreach (var parent in parentChain)
					{
						if (parent.id == _beatmapObject.id)
						{
							lists.Add(parentChain);
						}
					}
				}
			}
			return lists;
		}

		public static bool TimeWithinLifespan(this BeatmapObject _beatmapObject)
		{
			var time = AudioManager.inst.CurrentAudioSource.time;
			if (time >= _beatmapObject.StartTime && (AudioManager.inst.CurrentAudioSource.time <= _beatmapObject.GetObjectLifeLength() + _beatmapObject.StartTime && _beatmapObject.autoKillType != BeatmapObject.AutoKillType.OldStyleNoAutokill && _beatmapObject.autoKillType != BeatmapObject.AutoKillType.SongTime || AudioManager.inst.CurrentAudioSource.time < _beatmapObject.GetObjectLifeLength(0f, true) && _beatmapObject.autoKillType == BeatmapObject.AutoKillType.OldStyleNoAutokill || AudioManager.inst.CurrentAudioSource.time < _beatmapObject.autoKillOffset && _beatmapObject.autoKillType == BeatmapObject.AutoKillType.SongTime))
			{
				return true;
			}
			return false;
		}

		public static Color GetObjectColor(this BeatmapObject _beatmapObject, bool _ignoreTransparency)
        {
			if (_beatmapObject.objectType == BeatmapObject.ObjectType.Empty)
            {
				return Color.white;
            }

			if (ObjectManager.inst.beatmapGameObjects.ContainsKey(_beatmapObject.id) && ObjectManager.inst.beatmapGameObjects[_beatmapObject.id].obj != null && ObjectManager.inst.beatmapGameObjects[_beatmapObject.id].rend != null)
            {
				var gameObjectRef = ObjectManager.inst.beatmapGameObjects[_beatmapObject.id];

				Color color = Color.white;
				if (AudioManager.inst.CurrentAudioSource.time < _beatmapObject.StartTime)
				{
					color = GameManager.inst.LiveTheme.objectColors[(int)_beatmapObject.events[3][0].eventValues[0]];
				}
				else if (AudioManager.inst.CurrentAudioSource.time > _beatmapObject.StartTime + _beatmapObject.GetObjectLifeLength() && _beatmapObject.autoKillType != BeatmapObject.AutoKillType.OldStyleNoAutokill)
				{
					color = GameManager.inst.LiveTheme.objectColors[(int)_beatmapObject.events[3][_beatmapObject.events[3].Count - 1].eventValues[0]];
				}
				else
				{
					color = gameObjectRef.mat.color;
				}
				if (_ignoreTransparency)
				{
					color.a = 1f;
				}
				return color;
			}

			return Color.white;
		}

		public static Color GetPrefabTypeColor(this BeatmapObject _beatmapObject)
        {
			var prefab = DataManager.inst.gameData.prefabs.Find((DataManager.GameData.Prefab x) => x.ID == _beatmapObject.prefabID);
			return DataManager.inst.PrefabTypes[prefab.Type].Color;
        }

		public static int ClosestEventKeyframe(int _type)
		{
			var allEvents = DataManager.inst.gameData.eventObjects.allEvents;
			float time = AudioManager.inst.CurrentAudioSource.time;
			if (allEvents[_type].Find((DataManager.GameData.EventKeyframe x) => x.eventTime >= time) != null)
			{
				var nextKFE = allEvents[_type].Find((DataManager.GameData.EventKeyframe x) => x.eventTime >= time);
				var nextKF = allEvents[_type].IndexOf(nextKFE);
				var prevKF = nextKF - 1;

				if (nextKF == 0)
				{
					prevKF = 0;
				}
				else
				{
					var v1 = new Vector2(allEvents[_type][prevKF].eventTime, 0f);
					var v2 = new Vector2(allEvents[_type][nextKF].eventTime, 0f);

					float dis = Vector2.Distance(v1, v2) / 2f;

					bool prevClose = time > dis + allEvents[_type][prevKF].eventTime;
					bool nextClose = time < allEvents[_type][nextKF].eventTime - dis;

					if (!prevClose)
					{
						return prevKF;
					}
					if (!nextClose)
					{
						return nextKF;
					}
				}
			}
			return 0;
		}

		public static int ClosestKeyframe(this BeatmapObject beatmapObject, int _type)
		{
			if (beatmapObject.events[_type].Find((DataManager.GameData.EventKeyframe x) => x.eventTime >= AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime) != null)
			{
				var nextKFE = beatmapObject.events[_type].Find((DataManager.GameData.EventKeyframe x) => x.eventTime >= AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime);
				var nextKF = beatmapObject.events[_type].IndexOf(nextKFE);
				var prevKF = nextKF - 1;

				if (nextKF == 0)
				{
					prevKF = 0;
				}
				else
				{
					var v1 = new Vector2(beatmapObject.events[_type][prevKF].eventTime, 0f);
					var v2 = new Vector2(beatmapObject.events[_type][nextKF].eventTime, 0f);

					float dis = Vector2.Distance(v1, v2);
					float time = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

					bool prevClose = time > dis + beatmapObject.events[_type][prevKF].eventTime / 2f;
					bool nextClose = time < beatmapObject.events[_type][nextKF].eventTime - dis / 2f;

					if (!prevClose)
					{
						return prevKF;
					}
					if (!nextClose)
					{
						return nextKF;
					}
				}
			}
			return 0;
		}

		public static T AddComponent<T>(this Transform _transform) where T : Component
		{
			return _transform.gameObject.AddComponent(typeof(T)) as T;
		}

		public static void TestTransformAddComponent()
        {
			GameObject.Find("Test").transform.AddComponent<SaveManager>();
        }

		public static void Duplicate(this GameObject _gameObject, Transform _parent)
        {
            var copy = UnityEngine.Object.Instantiate(_gameObject, _parent);
            copy.transform.localScale = _gameObject.transform.localScale;
        }

        public static float GetPrefabLifeLength(this DataManager.GameData.Prefab _prefab, DataManager.GameData.PrefabObject _prefabObject, bool _collapse = false)
        {
			if (_collapse && _prefabObject.editorData.collapse)
			{
				return 0.2f;
			}
			float a;
			if (_prefab.prefabObjects.Count <= 0)
			{
				a = 0f;
			}
			else
			{
				a = (from x in _prefab.prefabObjects
					 orderby x.StartTime
					 select x).First().StartTime;
			}
			float b;
			if (_prefab.objects.Count <= 0)
			{
				b = 0f;
			}
			else
			{
				b = (from x in _prefab.objects
					 orderby x.StartTime
					 select x).First().StartTime;
			}
			float num = Mathf.Min(a, b);
			float num2 = 0f;
			foreach (DataManager.GameData.BeatmapObject beatmapObject in _prefab.objects)
			{
				float num3 = beatmapObject.StartTime + beatmapObject.GetObjectLifeLength(0f, false, false);
				num3 -= num;
				if (num2 < num3)
				{
					num2 = num3;
				}
			}
			using (List<DataManager.GameData.PrefabObject>.Enumerator enumerator2 = _prefab.prefabObjects.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					DataManager.GameData.PrefabObject obj = enumerator2.Current;
					float num4 = obj.StartTime + DataManager.inst.gameData.prefabs.Find((DataManager.GameData.Prefab x) => x.ID == obj.prefabID).GetLength();
					num4 -= num;
					if (num2 < num4)
					{
						num2 = num4;
					}
				}
			}
			return num2;
		}

		public static float StartTime(this ObjEditor.ObjectSelection _objectSelection)
        {
			if (_objectSelection.IsObject())
            {
				return _objectSelection.GetObjectData().StartTime;
            }
            if (_objectSelection.IsPrefab())
            {
                return _objectSelection.GetPrefabObjectData().StartTime;
            }

			return 0f;
		}

		public static object GetILevelObject(this BeatmapObject _beatmapObject)
		{
			var catalyst = GameObject.Find("BepInEx_Manager").GetComponentByName("CatalystBase");

			var instance = catalyst.GetType().GetField("Instance").GetValue(catalyst);

			var getILevelObject = instance.GetType().GetMethod("GetLevelObject");

			var obj = getILevelObject.Invoke(instance, new object[] { _beatmapObject });

			return obj;
		}

		public static bool StartCoroutine(IEnumerator _coroutine)
		{
			while (_coroutine.MoveNext())
			{
				return true;
			}
			return false;
		}

		public static string BoolToYN(this bool _bool)
		{
			if (_bool)
				return "Yes";
			return "No";
		}

		public static string ToWord(this int _int)
        {
			string str = "";
			if (_int.ToString().Length == 1)
            {
				switch (_int)
                {
					case 0:
                        {
							str = "Zero";
							break;
                        }
					case 1:
                        {
							str = "One";
							break;
                        }
					case 2:
                        {
							str = "Two";
							break;
                        }
					case 3:
                        {
							str = "Three";
							break;
                        }
					case 4:
                        {
							str = "Four";
							break;
                        }
					case 5:
                        {
							str = "Five";
							break;
                        }
					case 6:
                        {
							str = "Six";
							break;
                        }
					case 7:
                        {
							str = "Seven";
							break;
                        }
					case 8:
                        {
							str = "Eight";
							break;
                        }
					case 9:
                        {
							str = "Nine";
							break;
                        }
                }
            }
			if (_int.ToString().Length == 2)
            {
				string tw1 = _int.ToString().Substring(0, 1);
				string tw2 = _int.ToString().Substring(1, 1);
				int num1 = int.Parse(tw1);
				int num2 = int.Parse(tw2);

				string toWord2 = num2.ToWord();

				if (_int > 20)
                {
					switch (num1)
                    {
						case 2:
                            {
								str = "Twenty";
								if (toWord2.ToLower() != "zero")
                                {
									str += " " + toWord2;
                                }
								break;
                            }
						case 3:
                            {
								str = "Thirty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 4:
                            {
								str = "Fourty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 5:
                            {
								str = "Fifty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 6:
                            {
								str = "Sixty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 7:
                            {
								str = "Seventy";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 8:
                            {
								str = "Eighty";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
						case 9:
                            {
								str = "Ninety";
								if (toWord2.ToLower() != "zero")
								{
									str += " " + toWord2;
								}
								break;
                            }
                    }
                }
            }

			return str;
        }
	}
}
