using UnityEngine;

namespace NightCourier.AI
{
	public static class EnemyMath
	{
		private const float MinPlanarSqrMagnitude = 0.0001f;

		public static Vector3 StandoffPoint(Vector3 enemy, Vector3 target, float distance)
		{
			Vector3 away = enemy - target;
			away.y = 0f;

			if (away.sqrMagnitude < MinPlanarSqrMagnitude)
			{
				return enemy;
			}

			Vector3 point = target + away.normalized * distance;
			point.y = enemy.y;

			return point;
		}

		public static float PlanarDistance(Vector3 from, Vector3 to)
		{
			from.y = 0f;
			to.y = 0f;

			return Vector3.Distance(from, to);
		}

		public static bool ShouldFire(float planarDistance, float fireRange, bool hasLineOfSight)
		{
			return hasLineOfSight && planarDistance <= fireRange;
		}
	}
}
