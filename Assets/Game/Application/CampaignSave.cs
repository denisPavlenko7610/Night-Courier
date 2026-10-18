using System;
using System.Collections.Generic;
using NightCourier.Economy;
using NightCourier.Loadout;
using RD.Progression;
using RD.Quests;
using RD.Rewards;
using RD.Wallet;

namespace NightCourier.App
{
	[Serializable]
	public sealed class CampaignSave
	{
		public const int CurrentVersion = 5;
		public const int OldestSupportedVersion = 2;

		public int Version = CurrentVersion;
		public WalletSnapshot Wallet;
		public QuestServiceSnapshot Quests;
		public RewardLedgerSnapshot Rewards;
		public List<UpgradeLevelSave> Upgrades = new();
		public LoadoutSave Loadout;
		public ProgressionSnapshot Rank;
	}
}
