using System;
using System.Collections.Generic;
using UnityEngine;

namespace EditorManagement.Functions
{
	public static class RTMath
	{
		public static Vector3 CenterOfVectors(List<Vector3> vectors)
		{
			Vector3 vector = Vector3.zero;
			if (vectors == null || vectors.Count == 0)
			{
				return vector;
			}
			foreach (Vector3 b in vectors)
			{
				vector += b;
			}
			return vector / (float)vectors.Count;
		}

		public static float roundToNearest(float value, float multipleOf)
		{
			return (float)Math.Round((decimal)value / (decimal)multipleOf, MidpointRounding.AwayFromZero) * multipleOf;
		}

		public static Rect RectTransformToScreenSpace(RectTransform transform)
		{
			Vector2 vector = Vector2.Scale(transform.rect.size, transform.lossyScale);
			Rect result = new Rect(transform.position.x, (float)Screen.height - transform.position.y, vector.x, vector.y);
			result.x -= transform.pivot.x * vector.x;
			result.y -= (1f - transform.pivot.y) * vector.y;
			return result;
		}

		public static Rect RectTransformToScreenSpace2(RectTransform transform)
		{
			Vector2 vector = Vector2.Scale(transform.rect.size, transform.lossyScale);
			float x = transform.position.x + transform.anchoredPosition.x;
			float y = (float)Screen.height - transform.position.y - transform.anchoredPosition.y;
			return new Rect(x, y, vector.x, vector.y);
		}

		public static float InterpolateOverCurve(AnimationCurve curve, float from, float to, float t)
		{
			return from + curve.Evaluate(t) * (to - from);
		}

		public static Vector3 SphericalToCartesian(int radius, int polar)
		{
			return new Vector3
			{
				x = (float)radius * Mathf.Cos(0.017453292f * (float)polar),
				y = (float)radius * Mathf.Sin(0.017453292f * (float)polar)
			};
		}

		public static float SuperLerp(float OldMin, float OldMax, float NewMin, float NewMax, float OldValue)
		{
			float num = OldMax - OldMin;
			float num2 = NewMax - NewMin;
			return (OldValue - OldMin) * num2 / num + NewMin;
		}

		public static Rect ClampToScreen(Rect r)
		{
			r.x = Mathf.Clamp(r.x, 0f, (float)Screen.width - r.width);
			r.y = Mathf.Clamp(r.y, 0f, (float)Screen.height - r.height);
			return r;
		}

		public static float RoundToNearestDecimal(float _value, int _places = 3)
		{
			if (_places <= 0)
			{
				return Mathf.Round(_value);
			}
			int num = 10;
			for (int i = 1; i < _places; i++)
			{
				num *= 10;
			}
			return Mathf.Round(_value * (float)num) / (float)num;
		}
	}
}
