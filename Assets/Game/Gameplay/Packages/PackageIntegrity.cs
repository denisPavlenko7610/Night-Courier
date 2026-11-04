using System;
using RD.Health;
using RD.Mechanics.Combat;
using UnityEngine;

namespace NightCourier.Packages
{
	public sealed class PackageIntegrity : MonoBehaviour
	{
		[SerializeField] private HealthCarrier _health;

		public float Fraction => (float)_health.Pool.State.Normalized;

		public bool IsDestroyed => !_health.Pool.State.IsAlive;

		public event Action Changed;

		public event Action<string, float> Damaged;

		public event Action Destroyed;

		private void Awake()
		{
			_health.Pool.Changed += OnChanged;
			_health.Pool.Died += OnDied;
			_health.DamageResolved += OnDamageResolved;
		}

		private void OnDestroy()
		{
			if (_health == null)
			{
				return;
			}

			_health.Pool.Changed -= OnChanged;
			_health.Pool.Died -= OnDied;
			_health.DamageResolved -= OnDamageResolved;
		}

		private void OnDamageResolved(DamageRequest request, HealthResult result)
		{
			if (result.DamageApplied > 0d)
			{
				Damaged?.Invoke(request.Type.Value, (float)(result.DamageApplied / result.Current.Maximum));
			}
		}

		private void OnChanged(HealthResult result)
		{
			Changed?.Invoke();
		}

		private void OnDied()
		{
			Destroyed?.Invoke();
		}
	}
}
