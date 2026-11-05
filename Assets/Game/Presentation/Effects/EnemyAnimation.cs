using NightCourier.AI;
using RD.Health;
using RD.Mechanics.Combat;
using RD.Mechanics.MeleeAttack;
using UnityEngine;
using UnityEngine.AI;

namespace NightCourier.Presentation
{
	public sealed class EnemyAnimation : MonoBehaviour
	{
		private static readonly int SpeedId = Animator.StringToHash("Speed");
		private static readonly int HitId = Animator.StringToHash("Hit");
		private static readonly int AttackId = Animator.StringToHash("Attack");
		private static readonly int DieId = Animator.StringToHash("Die");

		[SerializeField] private Animator _animator;
		[SerializeField] private HealthCarrier _health;
		[SerializeField] private EnemyDeath _death;
		[SerializeField] private NavMeshAgent _agent;
		[SerializeField] private MeleeAttack _melee;
		[SerializeField] private SecurityDroneBrain _drone;
		[SerializeField, Min(0f)] private float _speedDampSeconds = 0.1f;

		private void OnEnable()
		{
			_health.DamageResolved += OnDamageResolved;
			_death.Died += OnDied;

			if (_melee != null)
			{
				_melee.AttackStarted += OnAttackStarted;
			}

			if (_drone != null)
			{
				_drone.BoltFired += OnAttackStarted;
			}
		}

		private void OnDisable()
		{
			if (_health != null)
			{
				_health.DamageResolved -= OnDamageResolved;
			}

			if (_death != null)
			{
				_death.Died -= OnDied;
			}

			if (_melee != null)
			{
				_melee.AttackStarted -= OnAttackStarted;
			}

			if (_drone != null)
			{
				_drone.BoltFired -= OnAttackStarted;
			}
		}

		private void Update()
		{
			if (_agent == null)
			{
				return;
			}

			float speed = _agent.isOnNavMesh ? _agent.velocity.magnitude : 0f;
			_animator.SetFloat(SpeedId, speed, _speedDampSeconds, Time.deltaTime);
		}

		private void OnDamageResolved(DamageRequest request, HealthResult result)
		{
			if (result.Delta < 0d && result.Current.IsAlive)
			{
				_animator.SetTrigger(HitId);
			}
		}

		private void OnAttackStarted()
		{
			_animator.SetTrigger(AttackId);
		}

		private void OnDied()
		{
			_animator.SetTrigger(DieId);
		}
	}
}
