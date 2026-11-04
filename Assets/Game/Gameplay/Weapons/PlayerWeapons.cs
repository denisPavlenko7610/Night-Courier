using System;
using NightCourier.Controls;
using NightCourier.Loadout;
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
		private CourierLoadout _loadout;
		private int _appliedReserveBonus;

		public WeaponKind Current { get; private set; }

		public StunPistol Pistol => _pistol;

		public bool HasEquippedWeapon => IsEquipped(WeaponKind.StunPistol) || IsEquipped(WeaponKind.Crowbar);

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
				actions.SwitchWeapon.SubscribePerformed(_ => SelectOther()),
				actions.Weapon1.SubscribePerformed(_ => Select(WeaponKind.StunPistol)),
				actions.Weapon2.SubscribePerformed(_ => Select(WeaponKind.Crowbar))
			});

			_switching.Switched += OnSwitched;
			_motor.BindRecoil(_pistol.Recoil);

			Current = _switching.CurrentIndex == WeaponSwitching.NoSelection
				? WeaponKind.StunPistol
				: (WeaponKind)_switching.CurrentIndex;
			Changed?.Invoke();
		}

		public void SetLoadout(CourierLoadout loadout)
		{
			if (_loadout != null)
			{
				_loadout.Changed -= OnLoadoutChanged;
			}

			_loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
			_loadout.Changed += OnLoadoutChanged;

			OnLoadoutChanged();
		}

		public bool IsEquipped(WeaponKind kind)
		{
			return _loadout == null || _loadout.IsWeaponEquipped(kind);
		}

		public void SetReserveBonus(int bonus)
		{
			int delta = bonus - _appliedReserveBonus;

			if (delta > 0)
			{
				_pistol.Ammo.AddReserve(delta);
			}

			_appliedReserveBonus = Math.Max(bonus, _appliedReserveBonus);
		}

		public void RefillAmmo(int rounds)
		{
			_pistol.Ammo.AddReserve(rounds);
		}

		private void OnDestroy()
		{
			_subscriptions?.Dispose();

			if (_loadout != null)
			{
				_loadout.Changed -= OnLoadoutChanged;
			}

			if (_switching != null)
			{
				_switching.Switched -= OnSwitched;
			}
		}

		private void Select(WeaponKind kind)
		{
			if (IsEquipped(kind))
			{
				_switching.Select((int)kind);
			}
		}

		private void SelectOther()
		{
			Select(Current == WeaponKind.StunPistol ? WeaponKind.Crowbar : WeaponKind.StunPistol);
		}

		private void OnLoadoutChanged()
		{
			if (!IsEquipped(Current))
			{
				SelectOther();
			}

			Changed?.Invoke();
		}

		private void Fire()
		{
			if (!IsEquipped(Current) || !TryUse(Current))
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
			if (Current == WeaponKind.StunPistol && IsEquipped(WeaponKind.StunPistol))
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
