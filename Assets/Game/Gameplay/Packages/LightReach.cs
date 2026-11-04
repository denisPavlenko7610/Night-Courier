using System;
using UnityEngine;

namespace NightCourier.Packages
{
	public readonly struct LightSource
	{
		public LightSource(LightType type, Vector3 position, Vector3 forward, float range, float spotAngle, bool castsShadows)
		{
			Type = type;
			Position = position;
			Forward = forward;
			Range = range;
			SpotAngle = spotAngle;
			CastsShadows = castsShadows;
		}

		public LightType Type { get; }

		public Vector3 Position { get; }

		public Vector3 Forward { get; }

		public float Range { get; }

		public float SpotAngle { get; }

		public bool CastsShadows { get; }
	}

	public static class LightReach
	{
		public const float DirectionalProbeDistance = 100f;

		public static bool Reaches(in LightSource light, Vector3 point, Func<Vector3, Vector3, bool> isBlocked)
		{
			if (isBlocked == null)
			{
				throw new ArgumentNullException(nameof(isBlocked));
			}

			switch (light.Type)
			{
				case LightType.Directional:
					return !light.CastsShadows || !isBlocked(point, point - light.Forward * DirectionalProbeDistance);
				case LightType.Point:
					return InRange(light, point) && !IsShadowed(light, point, isBlocked);
				case LightType.Spot:
					return InRange(light, point) && InCone(light, point) && !IsShadowed(light, point, isBlocked);
				default:
					return false;
			}
		}

		private static bool InRange(in LightSource light, Vector3 point)
		{
			return (light.Position - point).sqrMagnitude <= light.Range * light.Range;
		}

		private static bool InCone(in LightSource light, Vector3 point)
		{
			Vector3 toPoint = point - light.Position;

			if (toPoint.sqrMagnitude <= Mathf.Epsilon)
			{
				return true;
			}

			return Vector3.Angle(light.Forward, toPoint) <= light.SpotAngle * 0.5f;
		}

		private static bool IsShadowed(in LightSource light, Vector3 point, Func<Vector3, Vector3, bool> isBlocked)
		{
			return light.CastsShadows && isBlocked(light.Position, point);
		}
	}
}
