using System;
using NightCourier.Controls;
using NightCourier.Player;
using RD.Foundation;
using RD.Input;
using RD.Mechanics.WeaponSwitching;
using UnityEngine;

namespace NightCourier.Weapons
{
	public sealed class PlayerWeapons : MonoBehaviour
	{
		[SerializeField] private WeaponSwitching _switching;
		[SerializeField] private StunPistol _pistol;
		[SerializeField] private Crowbar _crowbar;
		[SerializeField] private PlayerCarry _packages;
		[SerializeField] private PlayerHeavyCarry _heavy;
		[SerializeField] private PlayerMotor _motor;
		[SerializeField] private Transform _aim;

		private IDisposable _subscriptions;

		public WeaponKind Current { get; private set; }

		public StunPistol Pistol => _pistol;

		public event Action Changed;

		public event Action<WeaponKind> UseRefused;

		public event Action ShotFired;

		public void Bind(PlayerInputActions actions)
		{
			if (actions == null)
			{
				throw new ArgumentNullException(nameof(actions));
			}

			_subscriptions?.Dispose();
			_switching.Switched -= OnSwitched;

			_subscriptions = new CompositeDisposable(new[]
			{
				actions.Fire.SubscribePerformed(_ => Fire()),
				actions.Reload.SubscribePerformed(_ => Reload()),
				actions.SwitchWeapon.SubscribePerformed(_ => _switching.SelectNext()),
				actions.Weapon1.SubscribePerformed(_ => _switching.Select((int)WeaponKind.StunPistol)),
				actions.Weapon2.SubscribePerformed(_ => _switching.Select((int)WeaponKind.Crowbar))
			});

			_switching.Switched += OnSwitched;
			_motor.BindRecoil(_pistol.Recoil);

			Current = _switching.CurrentIndex == WeaponSwitching.NoSelection
				? WeaponKind.StunPistol
				: (WeaponKind)_switching.CurrentIndex;
			Changed?.Invoke();
		}

		public void RefillAmmo(int rounds)
		{
			_pistol.Ammo.AddReserve(rounds);
		}

		private void OnDestroy()
		{
			_subscriptions?.Dispose();

			if (_switching != null)
			{
				_switching.Switched -= OnSwitched;
			}
		}

		private void Fire()
		{
			if (!TryUse(Current))
			{
				return;
			}

			if (Current == WeaponKind.StunPistol)
			{
				if (_pistol.TryFire(_aim))
				{
					ShotFired?.Invoke();
				}

				return;
			}

			_crowbar.TryAttack();
		}

		private void Reload()
		{
			if (Current == WeaponKind.StunPistol)
			{
				_pistol.Ammo.TryReload();
			}
		}

		private bool TryUse(WeaponKind kind)
		{
			if (WeaponRules.CanUse(kind, _packages.IsCarrying, _heavy.IsCarrying))
			{
				return true;
			}

			UseRefused?.Invoke(kind);

			return false;
		}

		private void OnSwitched(int index)
		{
			if (index == WeaponSwitching.NoSelection)
			{
				return;
			}

			_pistol.Ammo.CancelReload();
			Current = (WeaponKind)index;
			Changed?.Invoke();
		}
	}
}
