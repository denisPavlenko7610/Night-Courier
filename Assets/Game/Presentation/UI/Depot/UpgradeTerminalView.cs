using System;
using NightCourier.App;
using NightCourier.Audio;
using NightCourier.Economy;
using System.Collections.Generic;
using NightCourier.Player;
using RD.Foundation;
using RD.Localization;
using RD.Mechanics.Interactable;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class UpgradeTerminalView : MonoBehaviour
	{
		public const string PanelName = "upgrade-terminal";
		public const string CloseButtonName = "upgrade-close";

		private const string CreditsKey = "depot.credits";
		private const string CreditsName = "upgrade-credits";
		private const string RowName = "row";

		[SerializeField] private UiSounds _sounds;
		[SerializeField] private Interactable _terminal;
		[SerializeField] private UIDocument _document;

		private readonly List<UpgradeSlot> _slots = new();

		private VisualElement _panel;
		private Label _credits;
		private Button _close;

		private GameServices _services;
		private CampaignState _campaign;
		private PlayerSession _player;
		private IDisposable _scope;
		private bool _isBuying;

		public void Bind(GameServices services, CampaignState campaign, PlayerSession player)
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));
			_campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
			_player = player != null ? player : throw new ArgumentNullException(nameof(player));

			VisualElement root = _document.rootVisualElement;

			_panel = UiQuery.Require<VisualElement>(root, PanelName);
			_credits = UiQuery.Require<Label>(_panel, CreditsName);
			_close = UiQuery.Require<Button>(_panel, CloseButtonName);

			foreach (VisualElement row in _panel.Query<VisualElement>(RowName).ToList())
			{
				_slots.Add(new UpgradeSlot(row, services.Localization));
			}

			if (_campaign.Upgrades.Book.All.Count > _slots.Count)
			{
				throw new InvalidOperationException(
					$"The upgrade terminal has {_slots.Count} slots for {_campaign.Upgrades.Book.All.Count} upgrades.");
			}

			LocalizedText.Apply(_panel, services.Localization);

			_terminal.Interacted += Open;
			_sounds.Bind(services.Audio, _panel);
			_close.clicked += Close;
			UiQuery.SetVisible(_panel, false);
		}

		private void OnDestroy()
		{
			_scope?.Dispose();

			if (_terminal != null)
			{
				_terminal.Interacted -= Open;
			}

			if (_close != null)
			{
				_close.clicked -= Close;
			}

			foreach (UpgradeSlot slot in _slots)
			{
				slot.Dispose();
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

			foreach (UpgradeSlot slot in _slots)
			{
				if (slot.Button.enabledSelf && slot.Button.resolvedStyle.display != DisplayStyle.None)
				{
					slot.Button.Focus();
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
			long credits = _campaign.Credits;

			_credits.text = _services.Localization.Get(CreditsKey, credits);

			CourierUpgrades upgrades = _campaign.Upgrades;

			for (int index = 0; index < _slots.Count; index++)
			{
				if (index < upgrades.Book.All.Count)
				{
					UpgradeSpec spec = upgrades.Book.All[index];

					_slots[index].Show(spec, upgrades.GetLevel(spec.Id), credits, OnBuy);
				}
				else
				{
					_slots[index].Hide();
				}
			}
		}

		private async void OnBuy(UpgradeSpec spec)
		{
			if (_isBuying)
			{
				return;
			}

			_isBuying = true;

			try
			{
				if (_campaign.Upgrades.TryPurchase(spec.Id) == UpgradePurchaseStatus.Purchased)
				{
					_player.ApplyStats(_campaign.Stats);
					Refresh();

					await _services.Campaign.SaveAsync();
				}
				else
				{
					Refresh();
				}
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
			finally
			{
				_isBuying = false;
			}
		}
	}
}
