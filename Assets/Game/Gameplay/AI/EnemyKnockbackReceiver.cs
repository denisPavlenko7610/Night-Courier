using RD.Mechanics.Knockback;
using UnityEngine;
using UnityEngine.AI;

namespace NightCourier.AI
{
	public sealed class EnemyKnockbackReceiver : MonoBehaviour, IKnockbackReceiver
	{
		[SerializeField] private NavMeshAgent _agent;
		[SerializeField, Min(1f)] private float _decayPerSecond = 16f;

		private Vector3 _velocity;

		public void ApplyKnockback(Vector3 velocity)
		{
			_velocity = new Vector3(velocity.x, 0f, velocity.z);
		}

		private void Update()
		{
			if (_velocity == Vector3.zero)
			{
				return;
			}

			Vector3 step = _velocity * Time.deltaTime;

			if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
			{
				_agent.Move(step);
			}
			else
			{
				transform.position += step;
			}

			_velocity = Vector3.MoveTowards(_velocity, Vector3.zero, _decayPerSecond * Time.deltaTime);
		}
	}
}
