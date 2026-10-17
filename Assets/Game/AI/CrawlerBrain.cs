using RD.Mechanics.ChaseTargetMemory;
using RD.Mechanics.Combat;
using RD.Mechanics.DetectionSensor;
using RD.Mechanics.Knockback;
using RD.Mechanics.MeleeAttack;
using UnityEngine;
using UnityEngine.AI;

namespace NightCourier.AI
{
	public sealed class CrawlerBrain : MonoBehaviour
	{
		private const float HomeArrivalDistance = 0.6f;

		[SerializeField] private NavMeshAgent _agent;
		[SerializeField] private DetectionSensor _sensor;
		[SerializeField] private ChaseTargetMemory _memory;
		[SerializeField] private MeleeAttack _melee;
		[SerializeField] private Knockback _knockback;
		[SerializeField, Min(0.5f)] private float _attackRange = 1.4f;
		[SerializeField, Min(0.05f)] private float _repathSeconds = 0.2f;
		[SerializeField, Min(30f)] private float _turnDegreesPerSecond = 540f;

		private Vector3 _home;
		private float _repathRemaining;

		private void Awake()
		{
			_home = transform.position;
			_agent.updateRotation = false;

			_sensor.TargetDetected += _memory.ReportTarget;
			_sensor.TargetLost += _memory.ClearTarget;
			_melee.TargetHit += OnTargetHit;
		}

		private void OnDestroy()
		{
			if (_sensor != null)
			{
				_sensor.TargetDetected -= _memory.ReportTarget;
				_sensor.TargetLost -= _memory.ClearTarget;
			}

			if (_melee != null)
			{
				_melee.TargetHit -= OnTargetHit;
			}
		}

		private void Update()
		{
			if (!_agent.isOnNavMesh)
			{
				return;
			}

			switch (_memory.CurrentState)
			{
				case ChaseState.Chasing when _memory.Target != null:
					Pursue(_memory.Target.position, attack: true);
					break;
				case ChaseState.Searching:
					Pursue(_memory.LastKnownPosition, attack: false);
					break;
				default:
					ReturnHome();
					break;
			}
		}

		private void Pursue(Vector3 point, bool attack)
		{
			bool inRange = attack && EnemyMath.PlanarDistance(transform.position, point) <= _attackRange;

			_agent.isStopped = inRange || _melee.IsAttacking;

			if (inRange)
			{
				Face(point - transform.position);

				if (!_melee.IsAttacking)
				{
					_melee.TryAttack();
				}

				return;
			}

			if (_melee.IsAttacking)
			{
				return;
			}

			Repath(point);
			Face(_agent.desiredVelocity);
		}

		private void ReturnHome()
		{
			if (EnemyMath.PlanarDistance(transform.position, _home) <= HomeArrivalDistance)
			{
				_agent.isStopped = true;
				return;
			}

			_agent.isStopped = false;
			Repath(_home);
			Face(_agent.desiredVelocity);
		}

		private void Repath(Vector3 point)
		{
			_repathRemaining -= Time.deltaTime;

			if (_repathRemaining > 0f)
			{
				return;
			}

			_repathRemaining = _repathSeconds;
			_agent.SetDestination(point);
		}

		private void Face(Vector3 direction)
		{
			direction.y = 0f;

			if (direction.sqrMagnitude < 0.001f)
			{
				return;
			}

			transform.rotation = Quaternion.RotateTowards(
				transform.rotation,
				Quaternion.LookRotation(direction),
				_turnDegreesPerSecond * Time.deltaTime);
		}

		private void OnTargetHit(HealthCarrier carrier)
		{
			_knockback.Apply(carrier, transform.position);
		}
	}
}
