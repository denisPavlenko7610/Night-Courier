using System;
using NightCourier.App;
using NightCourier.Economy;
using NightCourier.Player;
using RD.Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class UpgradeTerminalView : MonoBehaviour
	{
		private const string CreditsKey = "depot.credits";

		[SerializeField] private Interactable _terminal;
		[SerializeField] private GameObject _panel;
		[SerializeField] private Button _closeButton;
		[SerializeField] private LocalizedLabel _creditsLabel;
		[SerializeField] private UpgradeSlot[] _slots;

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

			if (_campaign.Upgrades.Book.All.Count > _slots.Length)
			{
				throw new InvalidOperationException(
					$"The upgrade terminal has {_slots.Length} slots for {_campaign.Upgrades.Book.All.Count} upgrades.");
			}

			_terminal.Interacted += Open;
			_closeButton.onClick.AddListener(Close);
			_panel.SetActive(false);
		}

		private void OnDestroy()
		{
			_scope?.Dispose();

			if (_terminal != null)
			{
				_terminal.Interacted -= Open;
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
			long credits = _campaign.Credits;

			_creditsLabel.SetKey(CreditsKey, credits);

			CourierUpgrades upgrades = _campaign.Upgrades;

			for (int index = 0; index < _slots.Length; index++)
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
