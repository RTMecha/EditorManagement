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
        public static bool TimeWithinLifespan(this DataManager.GameData.BeatmapObject _beatmapObject)
        {
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (time >= _beatmapObject.StartTime && (time <= _beatmapObject.GetObjectLifeLength() && _beatmapObject.autoKillType != DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill && _beatmapObject.autoKillType != DataManager.GameData.BeatmapObject.AutoKillType.SongTime || time < _beatmapObject.GetObjectLifeLength(0f, true) && _beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill || time < _beatmapObject.autoKillOffset && _beatmapObject.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.SongTime))
            {
                return true;
            }
            return false;
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
    }
}
