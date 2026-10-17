using RD.Mechanics.ChaseTargetMemory;
using RD.Mechanics.DetectionSensor;
using RD.Mechanics.Patrol;
using RD.Mechanics.Projectile;
using RD.Timers;
using UnityEngine;
using UnityEngine.AI;

namespace NightCourier.AI
{
	public sealed class SecurityDroneBrain : MonoBehaviour
	{
		private const float NavMeshSnapDistance = 3f;
		private const float RouteArrivalDistance = 0.8f;

		[SerializeField] private NavMeshAgent _agent;
		[SerializeField] private Patrol _patrol;
		[SerializeField] private DetectionSensor _sensor;
		[SerializeField] private ChaseTargetMemory _memory;
		[SerializeField] private Transform _muzzle;
		[SerializeField] private Projectile _boltPrefab;
		[SerializeField] private Transform[] _route;
		[SerializeField] private LayerMask _sightMask = ~0;
		[SerializeField, Min(1f)] private float _standoffDistance = 6f;
		[SerializeField, Min(1f)] private float _fireRange = 14f;
		[SerializeField, Min(1f)] private float _boltSpeed = 18f;
		[SerializeField, Min(1f)] private float _boltDamage = 8f;
		[SerializeField, Min(0.1f)] private float _fireIntervalSeconds = 1.4f;
		[SerializeField, Min(0.05f)] private float _repathSeconds = 0.25f;
		[SerializeField, Min(30f)] private float _turnDegreesPerSecond = 240f;
		[SerializeField] private float _aimHeight = 1.1f;

		private Cooldown _fire;
		private bool _pursuing;
		private float _repathRemaining;

		public bool IsPursuing => _pursuing;

		private void Awake()
		{
			_fire = new Cooldown(_fireIntervalSeconds);
			_agent.updateRotation = false;
			_agent.enabled = false;

			_sensor.TargetDetected += _memory.ReportTarget;
			_sensor.TargetLost += _memory.ClearTarget;
		}

		private void OnDestroy()
		{
			if (_sensor != null)
			{
				_sensor.TargetDetected -= _memory.ReportTarget;
				_sensor.TargetLost -= _memory.ClearTarget;
			}
		}

		private void Update()
		{
			ChaseState state = _memory.CurrentState;

			if (state == ChaseState.Idle)
			{
				if (_pursuing)
				{
					ReturnToRoute();
				}

				return;
			}

			BeginPursuit();

			if (!_agent.isOnNavMesh)
			{
				return;
			}

			if (state == ChaseState.Chasing && _memory.Target != null)
			{
				Engage(_memory.Target.position);
			}
			else
			{
				Repath(_memory.LastKnownPosition);
				Face(_agent.desiredVelocity);
			}
		}

		private void BeginPursuit()
		{
			if (_pursuing && _agent.enabled)
			{
				return;
			}

			_patrol.enabled = false;

			if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSnapDistance, NavMesh.AllAreas))
			{
				transform.position = hit.position;
			}

			_agent.enabled = true;
			_pursuing = true;
		}

		private void Engage(Vector3 targetPosition)
		{
			Repath(EnemyMath.StandoffPoint(transform.position, targetPosition, _standoffDistance));
			Face(targetPosition - transform.position);

			Vector3 aim = targetPosition + Vector3.up * _aimHeight;
			bool hasLineOfSight = !Physics.Linecast(_muzzle.position, aim, _sightMask, QueryTriggerInteraction.Ignore);

			if (EnemyMath.ShouldFire(EnemyMath.PlanarDistance(transform.position, targetPosition), _fireRange, hasLineOfSight)
				&& _fire.TryStart())
			{
				Fire(aim);
			}
		}

		private void Fire(Vector3 aim)
		{
			Vector3 direction = (aim - _muzzle.position).normalized;
			Projectile bolt = Instantiate(_boltPrefab, _muzzle.position, Quaternion.LookRotation(direction));

			bolt.Configure(_boltDamage);
			bolt.Initialize(direction * _boltSpeed, transform);
		}

		private void ReturnToRoute()
		{
			if (!_agent.isOnNavMesh)
			{
				ResumePatrol();
				return;
			}

			Transform waypoint = NearestWaypoint();

			if (EnemyMath.PlanarDistance(transform.position, waypoint.position) <= RouteArrivalDistance)
			{
				ResumePatrol();
				return;
			}

			Repath(waypoint.position);
			Face(_agent.desiredVelocity);
		}

		private void ResumePatrol()
		{
			_agent.enabled = false;
			_patrol.enabled = true;
			_pursuing = false;
		}

		private Transform NearestWaypoint()
		{
			Transform best = _route[0];
			float bestDistance = float.MaxValue;

			foreach (Transform waypoint in _route)
			{
				float distance = EnemyMath.PlanarDistance(transform.position, waypoint.position);

				if (distance < bestDistance)
				{
					best = waypoint;
					bestDistance = distance;
				}
			}

			return best;
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
	}
}
