using System;
using System.Threading;
using NightCourier.Contracts;
using NightCourier.Economy;
using RD.Save;
using UnityEngine;

namespace NightCourier.App
{
	public sealed class CampaignRepository
	{
		public const string SlotId = "campaign";

		private readonly SaveStore _store;
		private readonly ContractBook _book;
		private readonly UpgradeBook _upgrades;

		public CampaignRepository(SaveStore store, ContractBook book, UpgradeBook upgrades)
		{
			_store = store ?? throw new ArgumentNullException(nameof(store));
			_book = book ?? throw new ArgumentNullException(nameof(book));
			_upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
		}

		public CampaignState Current { get; private set; }

		public bool HasSave { get; private set; }

		public async Awaitable RefreshAsync(CancellationToken cancellationToken = default)
		{
			HasSave = await _store.ExistsAsync(SlotId, cancellationToken);
		}

		public void StartNew()
		{
			Current = CampaignState.CreateNew(_book, _upgrades);
		}

		public async Awaitable ContinueAsync(CancellationToken cancellationToken = default)
		{
			CampaignSave save = await _store.LoadAsync<CampaignSave>(SlotId, cancellationToken);
			Current = CampaignState.Restore(_book, save, _upgrades);
		}

		public async Awaitable SaveAsync(CancellationToken cancellationToken = default)
		{
			if (Current == null)
			{
				throw new InvalidOperationException("There is no active campaign to save.");
			}

			await _store.SaveAsync(SlotId, Current.Capture(), cancellationToken);
			HasSave = true;
		}
	}
}
