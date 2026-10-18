using System;
using RD.Mechanics.AmmoReload;
using RD.Mechanics.Combat;
using RD.Mechanics.HitscanWeapon;
using RD.Mechanics.Recoil;
using RD.Mechanics.WeaponSpread;
using UnityEngine;

namespace NightCourier.Weapons
{
	public sealed class StunPistol : MonoBehaviour
	{
		[SerializeField] private HitscanWeapon _weapon;
		[SerializeField] private AmmoReload _ammo;
		[SerializeField] private Recoil _recoil;
		[SerializeField] private WeaponSpread _spread;
		[SerializeField] private Transform _muzzle;
		[SerializeField] private LineRenderer _tracer;
		[SerializeField] private LayerMask _aimMask = ~0;
		[SerializeField, Min(1f)] private float _aimRange = 60f;
		[SerializeField, Min(0.01f)] private float _tracerSeconds = 0.06f;

		private float _tracerRemaining;

		public AmmoReload Ammo => _ammo;

		public Recoil Recoil => _recoil;

		public event Action<CombatHit> Fired;

		public event Action DryFired;

		public bool TryFire(Transform aim)
		{
			if (aim == null)
			{
				throw new ArgumentNullException(nameof(aim));
			}

			if (_ammo.IsReloading || !_weapon.IsReadyToFire)
			{
				return false;
			}

			if (!_ammo.CanFire)
			{
				if (_ammo.CanReload)
				{
					_ammo.TryReload();
				}

				DryFired?.Invoke();

				return false;
			}

			Vector3 direction = _spread.Apply((ResolveAimPoint(aim) - _muzzle.position).normalized);

			if (!_weapon.TryFire(direction, out CombatHit hit))
			{
				return false;
			}

			_ammo.TryConsume();
			_recoil.Generate();
			ShowTracer(hit.Point);
			Fired?.Invoke(hit);

			return true;
		}

		private void Update()
		{
			if (_tracerRemaining <= 0f)
			{
				return;
			}

			_tracerRemaining -= Time.deltaTime;

			if (_tracerRemaining <= 0f)
			{
				_tracer.enabled = false;
			}
		}

		private void OnDisable()
		{
			_tracerRemaining = 0f;
			_tracer.enabled = false;
		}

		private Vector3 ResolveAimPoint(Transform aim)
		{
			return Physics.Raycast(aim.position, aim.forward, out RaycastHit hit, _aimRange, _aimMask, QueryTriggerInteraction.Ignore)
				? hit.point
				: aim.position + aim.forward * _aimRange;
		}

		private void ShowTracer(Vector3 end)
		{
			_tracer.SetPosition(0, _muzzle.position);
			_tracer.SetPosition(1, end);
			_tracer.enabled = true;
			_tracerRemaining = _tracerSeconds;
		}
	}
}
