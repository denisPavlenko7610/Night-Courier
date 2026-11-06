using System;
using NightCourier.Weapons;
using RD.Localization;
using RD.Mechanics.AmmoReload;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class WeaponHudView : MonoBehaviour
	{
		private const string AmmoKey = "hud.ammo";
		private const string ReloadingKey = "hud.reloading";
		private const string NoWeaponKey = "weapon.none";
		private const string WeaponName = "weapon-label";
		private const string AmmoRowName = "ammo-row";
		private const string AmmoName = "ammo-label";

		[SerializeField] private UIDocument _document;

		private ILocalizationService _localization;
		private PlayerWeapons _weapons;
		private AmmoReload _source;
		private Label _weapon;
		private VisualElement _ammoRow;
		private Label _ammo;

		public void Bind(ILocalizationService localization, PlayerWeapons weapons)
		{
			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			Unbind();

			VisualElement root = _document.rootVisualElement;

			_localization = localization;
			_weapons = weapons != null ? weapons : throw new ArgumentNullException(nameof(weapons));
			_source = _weapons.Pistol.Ammo;
			_weapon = UiQuery.Require<Label>(root, WeaponName);
			_ammoRow = UiQuery.Require<VisualElement>(root, AmmoRowName);
			_ammo = UiQuery.Require<Label>(root, AmmoName);

			_localization.LocaleChanged += OnLocaleChanged;
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

			_localization.LocaleChanged -= OnLocaleChanged;
			_weapons.Changed -= Refresh;
			_source.AmmoChanged -= OnAmmoEvent;
			_source.ReloadStarted -= OnAmmoEvent;
			_source.ReloadCompleted -= OnAmmoEvent;
			_source.ReloadCancelled -= OnAmmoEvent;
			_weapons = null;
			_source = null;
		}

		private void OnLocaleChanged(LocaleChanged change)
		{
			Refresh();
		}

		private void OnAmmoEvent(AmmoReload ammo)
		{
			Refresh();
		}

		private void Refresh()
		{
			if (!_weapons.HasEquippedWeapon)
			{
				_weapon.text = _localization.Get(NoWeaponKey);
				UiQuery.SetVisible(_ammoRow, false);
				return;
			}

			_weapon.text = _localization.Get(WeaponRules.NameKey(_weapons.Current));

			bool pistol = _weapons.Current == WeaponKind.StunPistol;

			UiQuery.SetVisible(_ammoRow, pistol);

			if (!pistol)
			{
				return;
			}

			_ammo.text = _source.IsReloading
				? _localization.Get(ReloadingKey)
				: _localization.Get(AmmoKey, _source.AmmoInMagazine, _source.ReserveAmmo);
		}
	}
}
