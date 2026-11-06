using System;
using System.Collections.Generic;
using NightCourier.App;
using NightCourier.Audio;
using NightCourier.Loadout;
using NightCourier.Weapons;
using RD.Equipment;
using RD.Foundation;
using RD.Localization;
using RD.Mechanics.Interactable;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class EquipmentLockerView : MonoBehaviour
	{
		public const string PanelName = "locker-terminal";
		public const string CloseButtonName = "locker-close";

		private const string CreditsKey = "depot.credits";
		private const string CreditsName = "locker-credits";
		private const string RowName = "row";
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

		[SerializeField] private UiSounds _sounds;
		[SerializeField] private Interactable _locker;
		[SerializeField] private UIDocument _document;

		private readonly List<LockerRow> _rows = new();

		private VisualElement _panel;
		private Label _credits;
		private Button _close;

		private GameServices _services;
		private CampaignState _campaign;
		private IDisposable _scope;
		private bool _isBusy;

		public void Bind(GameServices services, CampaignState campaign)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));

			VisualElement root = _document.rootVisualElement;

			_panel = UiQuery.Require<VisualElement>(root, PanelName);
			_credits = UiQuery.Require<Label>(_panel, CreditsName);
			_close = UiQuery.Require<Button>(_panel, CloseButtonName);

			foreach (VisualElement row in _panel.Query<VisualElement>(RowName).ToList())
			{
				_rows.Add(new LockerRow(row, services.Localization));
			}

			if (_rows.Count < RowCount)
			{
				throw new InvalidOperationException($"The equipment locker needs {RowCount} rows but has {_rows.Count}.");
			}

			LocalizedText.Apply(_panel, services.Localization);

			_locker.Interacted += Open;
			_sounds.Bind(services.Audio, _panel);
			_close.clicked += Close;
			UiQuery.SetVisible(_panel, false);
		}

		private void OnDestroy()
		{
			_scope?.Dispose();

			if (_locker != null)
			{
				_locker.Interacted -= Open;
			}

			if (_close != null)
			{
				_close.clicked -= Close;
			}

			foreach (LockerRow row in _rows)
			{
				row.Dispose();
			}
		}

		public bool IsOpen => _scope != null;

		public VisualElement Panel => _panel;

		private void Open()
		{
			if (_scope != null)
			{
				return;
			}

			_scope = _services.Input.PushMenu(Close);
			UiQuery.SetVisible(_panel, true);
			_sounds.PlayOpen();

			Refresh();
			_panel.schedule.Execute(FocusPrimary);
		}

		private void FocusPrimary()
		{
			if (_scope == null)
			{
				return;
			}

			for (int index = 0; index < RowCount; index++)
			{
				if (_rows[index].Button.enabledSelf)
				{
					_rows[index].Button.Focus();
					return;
				}
			}

			_close.Focus();
		}

		private void Close()
		{
			if (_scope == null)
			{
				return;
			}

			_scope.Dispose();
			_scope = null;
			UiQuery.SetVisible(_panel, false);
		}

		private void Refresh()
		{
			CourierLoadout loadout = _campaign.Loadout;
			long credits = _campaign.Credits;

			_credits.text = _services.Localization.Get(CreditsKey, credits);

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

			for (int index = RowCount; index < _rows.Count; index++)
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
