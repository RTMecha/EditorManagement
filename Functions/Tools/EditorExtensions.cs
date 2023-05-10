using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;

using SimpleJSON;

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

		public static GameObject GetGameObject(this DataManager.GameData.BeatmapObject _beatmapObject)
        {
			return _beatmapObject.GetTransformChain()[_beatmapObject.GetTransformChain().Count - 1].gameObject;
		}

		public static List<DataManager.GameData.BeatmapObject> GetParentChain(this DataManager.GameData.BeatmapObject _beatmapObject)
        {
			List<DataManager.GameData.BeatmapObject> beatmapObjects = new List<DataManager.GameData.BeatmapObject>();
			if (_beatmapObject != null)
			{
				var orig = _beatmapObject;
				beatmapObjects.Add(orig);

				while (!string.IsNullOrEmpty(orig.parent))
				{
					var select = DataManager.inst.gameData.beatmapObjects.Find((DataManager.GameData.BeatmapObject x) => x.id == orig.parent);
					beatmapObjects.Add(select);
					orig = select;
				}
			}

			return beatmapObjects;
		}

		public static List<Transform> GetTransformChain(this DataManager.GameData.BeatmapObject _beatmapObject)
        {
			var gameObjectRef = ObjectManager.inst.beatmapGameObjects[_beatmapObject.id];
			var tf = gameObjectRef.obj.transform;

			var list = new List<Transform>();
			list.Add(tf);

			while (tf.childCount != 0 && tf.GetChild(0) != null)
			{
				tf = tf.GetChild(0);
				list.Add(tf);
			}

			return list;
		}

		public static Dictionary<string, DataManager.GameData.BeatmapObject> GetParentDictionary(this DataManager.GameData.BeatmapObject _beatmapObject)
		{
			Dictionary<string, DataManager.GameData.BeatmapObject> beatmapObjects = new Dictionary<string, DataManager.GameData.BeatmapObject>();
			if (_beatmapObject != null)
			{
				var orig = _beatmapObject;
				beatmapObjects.Add(orig.id, orig);

				while (!string.IsNullOrEmpty(orig.parent))
				{
					var select = DataManager.inst.gameData.beatmapObjects.Find((DataManager.GameData.BeatmapObject x) => x.id == orig.parent);
					beatmapObjects.Add(select.id, select);
					orig = select;
				}
			}

			return beatmapObjects;
		}

		public static bool TimeWithinLifespan(this DataManager.GameData.BeatmapObject _beatmapObject)
		{
			var time = AudioManager.inst.CurrentAudioSource.time;
			if (time >= _beatmapObject.StartTime && (AudioManager.inst.CurrentAudioSource.time <= _beatmapObject.GetObjectLifeLength() + _beatmapObject.StartTime && _beatmapObject.autoKillType != DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill && _beatmapObject.autoKillType != DataManager.GameData.BeatmapObject.AutoKillType.SongTime || AudioManager.inst.CurrentAudioSource.time < _beatmapObject.GetObjectLifeLength(0f, true) && _beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill || AudioManager.inst.CurrentAudioSource.time < _beatmapObject.autoKillOffset && _beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.SongTime))
			{
				return true;
			}
			return false;
		}

		public static Color GetObjectColor(this DataManager.GameData.BeatmapObject _beatmapObject, bool _ignoreTransparency)
        {
			if (_beatmapObject.objectType == DataManager.GameData.BeatmapObject.ObjectType.Empty)
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
				else if (AudioManager.inst.CurrentAudioSource.time > _beatmapObject.StartTime + _beatmapObject.GetObjectLifeLength() && _beatmapObject.autoKillType != DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill)
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

		public static Color GetPrefabTypeColor(this DataManager.GameData.BeatmapObject _beatmapObject)
        {
			var prefab = DataManager.inst.gameData.prefabs.Find((DataManager.GameData.Prefab x) => x.ID == _beatmapObject.prefabID);
			return DataManager.inst.PrefabTypes[prefab.Type].Color;
        }

		public static int ClosestKeyframe(this DataManager.GameData.BeatmapObject beatmapObject, int _type)
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

		public static void AddComponent(this Transform _transform, Component _component)
        {
            _transform.gameObject.AddComponentInternal(_component.name);
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
    }
}
