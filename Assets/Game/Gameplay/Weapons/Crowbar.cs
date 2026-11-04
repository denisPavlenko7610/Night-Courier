using System;
using RD.Mechanics.Combat;
using RD.Mechanics.Knockback;
using RD.Mechanics.MeleeAttack;
using RD.Tween;
using UnityEngine;

namespace NightCourier.Weapons
{
	public sealed class Crowbar : MonoBehaviour
	{
		[SerializeField] private MeleeAttack _melee;
		[SerializeField] private Knockback _knockback;
		[SerializeField] private Transform _owner;
		[SerializeField] private Transform _swingPivot;
		[SerializeField] private Vector3 _swingEulerAngles = new(55f, 0f, 0f);
		[SerializeField, Min(0.02f)] private float _swingSeconds = 0.16f;
		[SerializeField, Min(0.02f)] private float _recoverSeconds = 0.3f;

		private Quaternion _restRotation;

		public bool IsSwinging => _melee.IsAttacking;

		public event Action Swung;

		public event Action<HealthCarrier> Hit;

		public bool TryAttack()
		{
			if (!_melee.TryAttack())
			{
				return false;
			}

			Swung?.Invoke();
			Swing();

			return true;
		}

		private void Awake()
		{
			_restRotation = _swingPivot.localRotation;
			_melee.TargetHit += OnTargetHit;
		}

		private void OnDestroy()
		{
			if (_melee != null)
			{
				_melee.TargetHit -= OnTargetHit;
			}

			Tween.Kill(_swingPivot);
		}

		private void OnDisable()
		{
			Tween.Kill(_swingPivot);
			_swingPivot.localRotation = _restRotation;
		}

		private void Swing()
		{
			Tween.Kill(_swingPivot);
			Tween.To(
					_swingPivot,
					() => _swingPivot.localRotation,
					value => _swingPivot.localRotation = value,
					_restRotation * Quaternion.Euler(_swingEulerAngles),
					_swingSeconds)
				.OnComplete(Recover);
		}

		private void Recover()
		{
			Tween.To(
				_swingPivot,
				() => _swingPivot.localRotation,
				value => _swingPivot.localRotation = value,
				_restRotation,
				_recoverSeconds);
		}

		private void OnTargetHit(HealthCarrier carrier)
		{
			_knockback.Apply(carrier, _owner.position);
			Hit?.Invoke(carrier);
		}
	}
}
