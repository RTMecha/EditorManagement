using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditorManagement
{
	public enum WaveformType
	{
		Legacy,
		Beta,
		LegacyFast,
		BetaFast
	}
	public enum Direction
	{
		Up,
		Down
	}
	public enum Easings
	{
		Linear,
		Instant,
		InSine,
		OutSine,
		InOutSine,
		InElastic,
		OutElastic,
		InOutElastic,
		InBack,
		OutBack,
		InOutBack,
		InBounce,
		OutBounce,
		InOutBounce,
		InQuad,
		OutQuad,
		InOutQuad,
		InCirc,
		OutCirc,
		InOutCirc,
		InExpo,
		OutExpo,
		InOutExpo
	}
	public enum Constraint
	{
		Flexible,
		FixedColumnCount,
		FixedRowCount
	}
	public enum PrefabDialog
	{
		Internal,
		External
	}
}
