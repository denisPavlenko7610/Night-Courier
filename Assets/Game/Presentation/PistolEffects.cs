using NightCourier.Weapons;
using RD.Mechanics.Combat;
using UnityEngine;

namespace NightCourier.Presentation
{
	public sealed class PistolEffects : MonoBehaviour
	{
		private const int SparkCount = 6;

		[SerializeField] private StunPistol _pistol;
		[SerializeField] private ParticleSystem _muzzleFlash;
		[SerializeField] private ParticleSystem _impactSparks;

		private void OnEnable()
		{
			_pistol.Fired += OnFired;
		}

		private void OnDisable()
		{
			if (_pistol != null)
			{
				_pistol.Fired -= OnFired;
			}
		}

		private void OnFired(CombatHit hit)
		{
			_muzzleFlash.Play();

			if (!hit.DidHit || hit.Normal.sqrMagnitude <= 0f)
			{
				return;
			}

			_impactSparks.transform.SetPositionAndRotation(hit.Point, Quaternion.LookRotation(hit.Normal));
			_impactSparks.Emit(SparkCount);
		}
	}
}
