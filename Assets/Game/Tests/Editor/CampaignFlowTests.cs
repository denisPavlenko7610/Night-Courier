#if UNITY_INCLUDE_TESTS
using System;
using System.Threading.Tasks;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Economy;
using NUnit.Framework;
using RD.Stats;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class CampaignFlowTests
	{
		private static CampaignSave RoundTrip(CampaignSave save)
		{
			return JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(save));
		}

		private static UpgradeBook Upgrades()
		{
			return new UpgradeBook(new[]
			{
				new UpgradeSpec("vest", "t", "d", CourierStats.MaxHealthId, StatModifierKind.Flat, 25d, new long[] { 50, 100 })
			});
		}

		private static ContractRun DriveToDelivered(ContractBoard board, string id)
		{
			ContractRun run = board.Accept(id);

			Assert.That(run.CollectPackage(), Is.True);
			Assert.That(run.Depart(), Is.True);
			run.Tick(5d);
			Assert.That(run.Deliver(), Is.True);

			return run;
		}

		[Test]
		public async Task AbortDuringSettlement_IsRefused_AndTheDeliveredRunStillPaysOnce()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());
			ContractRun run = DriveToDelivered(campaign.Contracts, "first");

			Assert.That(campaign.Contracts.Fail(), Is.False);
			Assert.That(run.Phase, Is.EqualTo(ContractPhase.Delivered));
			Assert.That(campaign.Contracts.ActiveRun, Is.SameAs(run));

			await campaign.Contracts.SettleAsync();

			Assert.That(campaign.Credits, Is.EqualTo(100));
			Assert.That(run.Phase, Is.EqualTo(ContractPhase.Completed));
		}

		[Test]
		public void AbandonUnfinished_ClearsADeliveredRunSoTheDepotCanStart()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());
			DriveToDelivered(campaign.Contracts, "first");

			campaign.Contracts.AbandonUnfinished();

			Assert.That(campaign.Contracts.ActiveRun, Is.Null);
			Assert.That(campaign.Credits, Is.EqualTo(0));
			Assert.Throws<InvalidOperationException>(() => campaign.Contracts.Accept("first"));
		}

		[Test]
		public async Task ConcurrentSettlement_PaysOnce()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());
			DriveToDelivered(campaign.Contracts, "first");

			Awaitable<ContractSettlement> first = campaign.Contracts.SettleAsync();
			Awaitable<ContractSettlement> second = campaign.Contracts.SettleAsync();

			await first;

			try
			{
				await second;
			}
			catch (InvalidOperationException)
			{
			}

			Assert.That(campaign.Credits, Is.EqualTo(100));
			Assert.That(campaign.Contracts.CompletedCount, Is.EqualTo(1));
		}

		[Test]
		public void SecondDeliveryCallback_IsRejectedWithoutChangingTheRun()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());
			ContractRun run = DriveToDelivered(campaign.Contracts, "first");
			double transit = run.TransitSeconds;

			Assert.That(run.Deliver(), Is.False);
			Assert.That(run.Phase, Is.EqualTo(ContractPhase.Delivered));
			Assert.That(run.TransitSeconds, Is.EqualTo(transit));
		}

		[Test]
		public async Task FailThenRetry_PaysExactlyOnce_AndNeverDuringTheFailure()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());
			ContractRun failed = campaign.Contracts.Accept("first");

			failed.CollectPackage();
			failed.Depart();
			failed.RegisterDeath();

			Assert.That(campaign.Contracts.Fail(), Is.True);
			Assert.That(campaign.Credits, Is.EqualTo(0));
			Assert.That(failed.Phase, Is.EqualTo(ContractPhase.Failed));

			DriveToDelivered(campaign.Contracts, "first");
			ContractSettlement settlement = await campaign.Contracts.SettleAsync();

			Assert.That(settlement.Total, Is.EqualTo(100));
			Assert.That(campaign.Credits, Is.EqualTo(100));
			Assert.That(campaign.Contracts.GetAvailable()[0].Id, Is.EqualTo("second"));
		}

		[Test]
		public async Task CompletedRunWithRankAndUpgrade_SurvivesSaveAndRestore_AndTheNextContractUnlocks()
		{
			ContractBook book = ContractFixtures.Chain();
			CampaignState original = CampaignState.CreateNew(book, Upgrades());

			ContractRun run = DriveToDelivered(original.Contracts, "first");
			ContractOutcome outcome = run.CaptureOutcome();
			ContractSettlement settlement = await original.Contracts.SettleAsync();
			RankChange rank = original.Rank.Apply(settlement.Rating);

			Assert.That(original.Upgrades.TryPurchase("vest"), Is.EqualTo(UpgradePurchaseStatus.Purchased));

			CampaignState restored = CampaignState.Restore(book, RoundTrip(original.Capture()), Upgrades());

			Assert.That(outcome.PackageViolationCount, Is.EqualTo(0));
			Assert.That(restored.Credits, Is.EqualTo(50));
			Assert.That(restored.Rank.TotalExperience, Is.EqualTo(rank.ExperienceGained));
			Assert.That(restored.Upgrades.GetLevel("vest"), Is.EqualTo(1));
			Assert.That(restored.Contracts.ActiveRun, Is.Null);
			Assert.That(restored.Contracts.GetAvailable()[0].Id, Is.EqualTo("second"));

			await Complete(restored, "second");

			Assert.That(restored.Credits, Is.EqualTo(250));
		}

		[Test]
		public void SaveWhileAContractIsInTransit_RestoresToADepotWithoutAnActiveRun()
		{
			ContractBook book = ContractFixtures.Chain();
			CampaignState original = CampaignState.CreateNew(book);
			ContractRun run = original.Contracts.Accept("first");

			run.CollectPackage();
			run.Depart();

			CampaignState restored = CampaignState.Restore(book, RoundTrip(original.Capture()));

			Assert.That(restored.Contracts.ActiveRun, Is.Null);
			Assert.That(restored.Credits, Is.EqualTo(0));
			Assert.That(restored.Contracts.GetAvailable()[0].Id, Is.EqualTo("first"));
		}

		private static async Task Complete(CampaignState campaign, string id)
		{
			DriveToDelivered(campaign.Contracts, id);
			await campaign.Contracts.SettleAsync();
		}
	}
}
#endif
