using NightCourier.AI;
using RD.Health;
using RD.Mechanics.Combat;
using UnityEngine;

namespace NightCourier.Presentation
{
	public sealed class EnemyEffects : MonoBehaviour
	{
		private const float SmokeLifetimeSeconds = 2.5f;

		[SerializeField] private HealthCarrier _health;
		[SerializeField] private EnemyDeath _death;
		[SerializeField] private ParticleSystem _hitSparks;
		[SerializeField] private ParticleSystem _deathSmoke;

		private void OnEnable()
		{
			_health.DamageResolved += OnDamageResolved;
			_death.Died += OnDied;
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
		}

		private void OnDamageResolved(DamageRequest request, HealthResult result)
		{
			if (result.Delta < 0d && result.Current.IsAlive)
			{
				_hitSparks.Play();
			}
		}

		private void OnDied()
		{
			_deathSmoke.transform.SetParent(null, true);
			_deathSmoke.Play();
			Destroy(_deathSmoke.gameObject, SmokeLifetimeSeconds);
		}
	}
}
