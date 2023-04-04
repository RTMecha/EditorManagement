using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace EditorManagement.Functions.Tools
{
    public static class EditorExtensions
    {
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
