using System;
using RD.Health;
using RD.Mechanics.ChaseTargetMemory;
using RD.Mechanics.Combat;
using UnityEngine;

namespace NightCourier.AI
{
	public sealed class EnemyAlert : MonoBehaviour
	{
		[SerializeField] private HealthCarrier _health;
		[SerializeField] private ChaseTargetMemory _memory;

		private Transform _attacker;

		public void Bind(Transform attacker)
		{
			_attacker = attacker != null ? attacker : throw new ArgumentNullException(nameof(attacker));
		}

		private void Awake()
		{
			_health.DamageResolved += OnDamageResolved;
		}

		private void OnDestroy()
		{
			if (_health != null)
			{
				_health.DamageResolved -= OnDamageResolved;
			}
		}

		private void OnDamageResolved(DamageRequest request, HealthResult result)
		{
			if (_attacker == null || result.Delta >= 0d || !result.Current.IsAlive || !_health.IncomingDirection.HasValue)
			{
				return;
			}

			_memory.ReportTarget(_attacker);
		}
	}
}
