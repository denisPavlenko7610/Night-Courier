using System;
using System.Collections.Generic;
using NightCourier.Contracts;
using NightCourier.Economy;
using RD.Quests;
using RD.Rewards;
using RD.Save;
using RD.Wallet;

namespace NightCourier.App
{
	public sealed class CampaignState
	{
		private readonly MemoryRewardGrantLedger _rewardLedger;

		private CampaignState(ContractBook book)
		{
			Wallet = new RD.Wallet.Wallet(new[] { GameCurrencies.Credits });

			List<QuestDefinition> definitions = new();

			foreach (ContractSpec spec in book.All)
			{
				definitions.Add(ContractQuests.Build(spec));
			}

			Quests = new QuestService(definitions);
			_rewardLedger = new MemoryRewardGrantLedger();

			RewardGrantService rewards = new(new IRewardHandler[] { new WalletCreditRewardHandler(Wallet) }, _rewardLedger);

			Contracts = new ContractBoard(book, Quests, rewards);
		}

		public IWallet Wallet { get; }

		public QuestService Quests { get; }

		public ContractBoard Contracts { get; }

		public long Credits => Wallet.GetBalance(GameCurrencies.Credits);

		public static CampaignState CreateNew(ContractBook book)
		{
			if (book == null)
			{
				throw new ArgumentNullException(nameof(book));
			}

			return new CampaignState(book);
		}

		public static CampaignState Restore(ContractBook book, CampaignSave save)
		{
			if (book == null)
			{
				throw new ArgumentNullException(nameof(book));
			}

			if (save == null || save.Version != CampaignSave.CurrentVersion
				|| save.Wallet == null || save.Quests == null || save.Rewards == null)
			{
				throw new InvalidSaveDataException("The campaign save is incomplete or has an unsupported version.");
			}

			CampaignState state = new(book);

			WalletSnapshotRestoreResult walletResult = state.Wallet.Restore(save.Wallet);

			if (!walletResult.IsSuccess)
			{
				throw new InvalidSaveDataException($"The wallet could not be restored: {walletResult.Message}");
			}

			state.Quests.RestoreSnapshot(
				save.Quests,
				new QuestRestoreOptions { IgnoreUnknownQuests = true, IgnoreUnknownObjectives = true });

			state._rewardLedger.RestoreSnapshot(save.Rewards);
			state.Contracts.AbandonUnfinished();

			return state;
		}

		public CampaignSave Capture()
		{
			return new CampaignSave
			{
				Wallet = Wallet.CaptureSnapshot(),
				Quests = Quests.CaptureSnapshot(),
				Rewards = _rewardLedger.CaptureSnapshot()
			};
		}
	}
}
