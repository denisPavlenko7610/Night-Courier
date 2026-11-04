using System;
using RD.Mechanics.Combat;
using RD.Tween;
using UnityEngine;
using UnityEngine.AI;

namespace NightCourier.AI
{
	public sealed class EnemyDeath : MonoBehaviour
	{
		[SerializeField] private HealthCarrier _health;
		[SerializeField] private Behaviour[] _stopOnDeath = Array.Empty<Behaviour>();
		[SerializeField] private Collider[] _colliders = Array.Empty<Collider>();
		[SerializeField] private NavMeshAgent _agent;
		[SerializeField, Min(0.1f)] private float _fadeSeconds = 0.6f;

		public bool IsDead { get; private set; }

		public event Action Died;

		private void Awake()
		{
			_health.Died += OnDied;
		}

		private void OnDestroy()
		{
			if (_health != null)
			{
				_health.Died -= OnDied;
			}

			Tween.Kill(transform);
		}

		private void OnDied(HealthCarrier carrier)
		{
			if (IsDead)
			{
				return;
			}

			IsDead = true;

			foreach (Behaviour behaviour in _stopOnDeath)
			{
				behaviour.enabled = false;
			}

			foreach (Collider collider in _colliders)
			{
				collider.enabled = false;
			}

			if (_agent != null && _agent.isActiveAndEnabled)
			{
				_agent.isStopped = true;
			}

			Died?.Invoke();

			Tween.To(transform, () => transform.localScale, value => transform.localScale = value, Vector3.zero, _fadeSeconds)
				.OnComplete(() => Destroy(gameObject));
		}
	}
}
