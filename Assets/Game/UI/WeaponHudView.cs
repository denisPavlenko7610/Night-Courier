using System;
using NightCourier.Weapons;
using RD.Localization;
using RD.Mechanics.AmmoReload;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class WeaponHudView : MonoBehaviour
	{
		private const string AmmoKey = "hud.ammo";
		private const string ReloadingKey = "hud.reloading";

		[SerializeField] private LocalizedLabel _weapon;
		[SerializeField] private LocalizedLabel _ammo;

		private PlayerWeapons _weapons;
		private AmmoReload _source;

		public void Bind(ILocalizationService localization, PlayerWeapons weapons)
		{
			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			Unbind();

			_weapons = weapons != null ? weapons : throw new ArgumentNullException(nameof(weapons));
			_source = _weapons.Pistol.Ammo;

			_weapon.Bind(localization);
			_ammo.Bind(localization);

			_weapons.Changed += Refresh;
			_source.AmmoChanged += OnAmmoEvent;
			_source.ReloadStarted += OnAmmoEvent;
			_source.ReloadCompleted += OnAmmoEvent;
			_source.ReloadCancelled += OnAmmoEvent;

			Refresh();
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_weapons == null)
			{
				return;
			}

			_weapons.Changed -= Refresh;
			_source.AmmoChanged -= OnAmmoEvent;
			_source.ReloadStarted -= OnAmmoEvent;
			_source.ReloadCompleted -= OnAmmoEvent;
			_source.ReloadCancelled -= OnAmmoEvent;
			_weapons = null;
			_source = null;
		}

		private void OnAmmoEvent(AmmoReload ammo)
		{
			Refresh();
		}

		private void Refresh()
		{
			_weapon.SetKey(WeaponRules.NameKey(_weapons.Current));

			bool pistol = _weapons.Current == WeaponKind.StunPistol;

			_ammo.gameObject.SetActive(pistol);

			if (!pistol)
			{
				return;
			}

			if (_source.IsReloading)
			{
				_ammo.SetKey(ReloadingKey);
			}
			else
			{
				_ammo.SetKey(AmmoKey, _source.AmmoInMagazine, _source.ReserveAmmo);
			}
		}
	}
}
