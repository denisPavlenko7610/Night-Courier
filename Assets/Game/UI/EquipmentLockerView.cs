using System;
using NightCourier.App;
using NightCourier.Loadout;
using NightCourier.Player;
using NightCourier.Weapons;
using RD.Equipment;
using RD.Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class EquipmentLockerView : MonoBehaviour
	{
		private const string CreditsKey = "depot.credits";
		private const string EquippedKey = "loadout.equipped";
		private const string StowedKey = "loadout.stowed";
		private const string EquipKey = "loadout.equip";
		private const string StowKey = "loadout.stow";
		private const string MedkitTitleKey = "loadout.medkit_stock";
		private const string MedkitStatusKey = "loadout.medkit_status";
		private const string BuyKey = "loadout.buy";
		private const string FullKey = "loadout.full";
		private const string MedkitKey = "loadout.medkit";
		private const string EmptyKey = "loadout.empty";
		private const string EquipMedkitKey = "loadout.equip_medkit";
		private const string UnequipKey = "loadout.unequip";
		private const string FirstUtilityKey = "loadout.utility_1";
		private const string SecondUtilityKey = "loadout.utility_2";

		private const int PistolRow = 0;
		private const int CrowbarRow = 1;
		private const int MedkitStockRow = 2;
		private const int FirstUtilityRow = 3;
		private const int SecondUtilityRow = 4;
		private const int RowCount = 5;

		[SerializeField] private Interactable _locker;
		[SerializeField] private GameObject _panel;
		[SerializeField] private Button _closeButton;
		[SerializeField] private LocalizedLabel _creditsLabel;
		[SerializeField] private LockerRow[] _rows;

		private GameServices _services;
		private CampaignState _campaign;
		private IDisposable _scope;
		private bool _isBusy;

		public void Bind(GameServices services, CampaignState campaign)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));

			if (_rows.Length < RowCount)
			{
				throw new InvalidOperationException($"The equipment locker needs {RowCount} rows but has {_rows.Length}.");
			}

			_locker.Interacted += Open;
			_closeButton.onClick.AddListener(Close);
			_panel.SetActive(false);
		}

		private void OnDestroy()
		{
			_scope?.Dispose();

			if (_locker != null)
			{
				_locker.Interacted -= Open;
			}

			_closeButton.onClick.RemoveListener(Close);
		}

		private void Open()
		{
			if (_scope != null)
			{
				return;
			}

			_scope = _services.Input.PushMenu(Close);
			_panel.SetActive(true);

			Refresh();
		}

		private void Close()
		{
			if (_scope == null)
			{
				return;
			}

			_scope.Dispose();
			_scope = null;
			_panel.SetActive(false);
		}

		private void Refresh()
		{
			CourierLoadout loadout = _campaign.Loadout;
			long credits = _campaign.Credits;

			_creditsLabel.SetKey(CreditsKey, credits);

			ShowWeapon(PistolRow, WeaponKind.StunPistol, loadout);
			ShowWeapon(CrowbarRow, WeaponKind.Crowbar, loadout);

			bool atCapacity = loadout.MedkitsOwned >= LoadoutRules.MedkitCapacity;

			_rows[MedkitStockRow].Show(
				MedkitTitleKey,
				MedkitStatusKey,
				new object[] { loadout.MedkitsInStock, loadout.MedkitsEquipped },
				atCapacity ? FullKey : BuyKey,
				new object[] { LoadoutRules.MedkitPrice },
				!atCapacity && credits >= LoadoutRules.MedkitPrice,
				OnBuyMedkit);

			ShowUtility(FirstUtilityRow, FirstUtilityKey, LoadoutIds.UtilityFirst, loadout);
			ShowUtility(SecondUtilityRow, SecondUtilityKey, LoadoutIds.UtilitySecond, loadout);

			for (int index = RowCount; index < _rows.Length; index++)
			{
				_rows[index].Hide();
			}
		}

		private void ShowWeapon(int row, WeaponKind kind, CourierLoadout loadout)
		{
			bool equipped = loadout.IsWeaponEquipped(kind);

			_rows[row].Show(
				WeaponRules.NameKey(kind),
				equipped ? EquippedKey : StowedKey,
				Array.Empty<object>(),
				equipped ? StowKey : EquipKey,
				Array.Empty<object>(),
				true,
				() => Apply(() => loadout.SetWeaponEquipped(kind, !equipped)));
		}

		private void ShowUtility(int row, string titleKey, EquipmentSlotId slot, CourierLoadout loadout)
		{
			bool hasMedkit = loadout.HasMedkit(slot);

			_rows[row].Show(
				titleKey,
				hasMedkit ? MedkitKey : EmptyKey,
				Array.Empty<object>(),
				hasMedkit ? UnequipKey : EquipMedkitKey,
				Array.Empty<object>(),
				hasMedkit || loadout.MedkitsInStock > 0,
				() => Apply(() => ToggleMedkit(loadout, slot)));
		}

		private static void ToggleMedkit(CourierLoadout loadout, EquipmentSlotId slot)
		{
			if (!loadout.TryUnequipMedkit(slot))
			{
				loadout.TryEquipMedkit(slot);
			}
		}

		private void OnBuyMedkit()
		{
			Apply(() => _campaign.Loadout.TryBuyMedkit());
		}

		private async void Apply(Action change)
		{
			if (_isBusy)
			{
				return;
			}

			_isBusy = true;

			try
			{
				change();
				Refresh();

				await _services.Campaign.SaveAsync();
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
			finally
			{
				_isBusy = false;
			}
		}
	}
}
