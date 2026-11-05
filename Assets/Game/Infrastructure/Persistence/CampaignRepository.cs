using System;
using System.Threading;
using NightCourier.Contracts;
using NightCourier.Economy;
using RD.Save;
using RD.Foundation;
using UnityEngine;

namespace NightCourier.App
{
	public sealed class CampaignRepository
	{
		public const string SlotId = "campaign";

		private readonly SaveStore _store;
		private readonly ContractBook _book;
		private readonly UpgradeBook _upgrades;
		private readonly AsyncGate _settlementGate = new();

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
			await RecoverSettlementAsync(cancellationToken);
		}

		public async Awaitable<RankChange> CompleteSettlementAsync(CancellationToken cancellationToken = default)
		{
			using (await _settlementGate.EnterAsync(cancellationToken))
			{
				if (Current?.Contracts.PendingSettlement == null)
				{
					throw new InvalidOperationException("There is no pending settlement.");
				}
				CampaignState campaign = Current;
				await SaveAsync(cancellationToken);
				if (!ReferenceEquals(Current, campaign))
				{
					throw new OperationCanceledException("The campaign changed during settlement.");
				}
				return await campaign.CompleteSettlementAsync(cancellationToken);
			}
		}

		public async Awaitable RecoverSettlementAsync(CancellationToken cancellationToken = default)
		{
			if (Current?.Contracts.PendingSettlement == null)
			{
				return;
			}
			CampaignState campaign = Current;
			PendingSettlement pending = campaign.Contracts.PendingSettlement.Copy();
			await CompleteSettlementAsync(cancellationToken);
			try
			{
				if (!ReferenceEquals(Current, campaign))
				{
					throw new OperationCanceledException("The campaign changed during settlement.");
				}
				await SaveAsync(cancellationToken);
			}
			catch
			{
				campaign.Contracts.RestoreSettlement(pending);
				throw;
			}
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
