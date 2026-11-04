using RD.Mechanics.Knockback;
using UnityEngine;

namespace NightCourier.Player
{
	public sealed class PlayerKnockbackReceiver : MonoBehaviour, IKnockbackReceiver
	{
		[SerializeField] private PlayerMotor _motor;

		public void ApplyKnockback(Vector3 velocity)
		{
			_motor.AddKnockback(velocity);
		}
	}
}
