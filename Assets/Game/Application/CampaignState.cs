using System;
using System.Collections.Generic;
using NightCourier.Contracts;
using NightCourier.Economy;
using NightCourier.Loadout;
using RD.Quests;
using RD.Rewards;
using RD.Save;
using RD.Wallet;

namespace NightCourier.App
{
	public sealed class CampaignState
	{
		private readonly MemoryRewardGrantLedger _rewardLedger;

		private CampaignState(ContractBook book, UpgradeBook upgrades)
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
			Stats = new CourierStats();
			Upgrades = new CourierUpgrades(upgrades, Wallet, Stats);
			Loadout = new CourierLoadout(Wallet);
		}

		public IWallet Wallet { get; }

		public QuestService Quests { get; }

		public ContractBoard Contracts { get; }

		public CourierStats Stats { get; }

		public CourierUpgrades Upgrades { get; }

		public CourierLoadout Loadout { get; }

		public long Credits => Wallet.GetBalance(GameCurrencies.Credits);

		public static CampaignState CreateNew(ContractBook book, UpgradeBook upgrades = null)
		{
			if (book == null)
			{
				throw new ArgumentNullException(nameof(book));
			}

			return new CampaignState(book, upgrades ?? UpgradeBook.Empty);
		}

		public static CampaignState Restore(ContractBook book, CampaignSave save, UpgradeBook upgrades = null)
		{
			if (book == null)
			{
				throw new ArgumentNullException(nameof(book));
			}

			if (save == null || save.Version < CampaignSave.OldestSupportedVersion || save.Version > CampaignSave.CurrentVersion
				|| save.Wallet == null || save.Quests == null || save.Rewards == null)
			{
				throw new InvalidSaveDataException("The campaign save is incomplete or has an unsupported version.");
			}

			CampaignState state = new(book, upgrades ?? UpgradeBook.Empty);

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
			state.Upgrades.Restore(save.Upgrades);
			state.Loadout.Restore(save.Loadout);

			return state;
		}

		public CampaignSave Capture()
		{
			return new CampaignSave
			{
				Wallet = Wallet.CaptureSnapshot(),
				Quests = Quests.CaptureSnapshot(),
				Rewards = _rewardLedger.CaptureSnapshot(),
				Upgrades = Upgrades.Capture(),
				Loadout = Loadout.Capture()
			};
		}
	}
}
