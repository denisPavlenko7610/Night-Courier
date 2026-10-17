using UnityEngine;

namespace NightCourier.Player
{
	public static class PlayerLookMath
	{
		public static float ClampPitch(float currentPitch, float deltaPitch, float limitDegrees)
		{
			return Mathf.Clamp(currentPitch + deltaPitch, -limitDegrees, limitDegrees);
		}

		public static Vector3 PlanarVelocity(Vector2 moveInput, Quaternion yaw, float speed)
		{
			Vector2 clamped = Vector2.ClampMagnitude(moveInput, 1f);
			Vector3 local = new(clamped.x, 0f, clamped.y);

			return yaw * local * speed;
		}
	}
}
