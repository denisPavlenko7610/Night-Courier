using System;
using RD.Mechanics.DamageOverTime;
using RD.StatusEffects;
using UnityEngine;

namespace NightCourier.Packages
{
	public sealed class PackageHazardEffect : MonoBehaviour
	{
		public const string ContaminationEffectId = "contamination";

		private const float ReachHeight = 1f;

		[SerializeField] private GameObject _aura;
		[SerializeField, Min(0.5f)] private float _radius = 3.5f;
		[SerializeField, Min(0.1f)] private float _checkSeconds = 0.4f;

		private DamageOverTime _target;
		private float _untilCheck;

		public bool IsActive => enabled;

		public float Radius => _radius;

		public void Activate(DamageOverTime target)
		{
			if (enabled)
			{
				return;
			}

			_target = target != null ? target : throw new ArgumentNullException(nameof(target));
			_aura.transform.localScale = Vector3.one * (_radius * 2f);
			_aura.SetActive(true);
			_untilCheck = 0f;
			enabled = true;
		}

		private void Update()
		{
			_untilCheck -= Time.deltaTime;

			if (_untilCheck > 0f)
			{
				return;
			}

			_untilCheck = _checkSeconds;

			Vector3 reach = _target.transform.position + Vector3.up * ReachHeight;

			if ((reach - transform.position).sqrMagnitude <= _radius * _radius)
			{
				_target.Apply(new StatusEffectId(ContaminationEffectId));
			}
		}
	}
}
