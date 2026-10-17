using System;
using RD.Quests;
using RD.Rewards;
using RD.Wallet;

namespace NightCourier.App
{
	[Serializable]
	public sealed class CampaignSave
	{
		public const int CurrentVersion = 2;

		public int Version = CurrentVersion;
		public WalletSnapshot Wallet;
		public QuestServiceSnapshot Quests;
		public RewardLedgerSnapshot Rewards;
	}
}
