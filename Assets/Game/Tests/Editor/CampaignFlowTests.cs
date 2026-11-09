#if UNITY_INCLUDE_TESTS
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using RD.Rewards;
using RD.Save;
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

		[TestCase(false)]
		[TestCase(true)]
		public async Task PendingSettlement_RewardFailureBeforeOrAfterMutation_RestoresAndPaysRankOnce(bool afterMutation)
		{
			ContractBook book = ContractFixtures.Chain();
			CampaignState campaign = CampaignState.CreateNew(book);
			DriveToDelivered(campaign.Contracts, "first");
			campaign.PrepareSettlement();
			RewardGrantService rewards = (RewardGrantService)typeof(ContractBoard).GetField("_rewards", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(campaign.Contracts);
			var handlers = (Dictionary<RewardTypeId, IRewardHandler>)typeof(RewardGrantService).GetField("_handlers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rewards);
			handlers[WalletCreditRewardHandler.RewardType] = new FailingCreditHandler(handlers[WalletCreditRewardHandler.RewardType], afterMutation);
			Assert.ThrowsAsync<InvalidOperationException>(async () => await campaign.CompleteSettlementAsync());
			Assert.That(campaign.Contracts.PendingSettlement, Is.Not.Null);
			Assert.That(campaign.Contracts.GetAvailable(), Is.Empty);
			Assert.That(campaign.Rank.TotalExperience, Is.Zero);
			Assert.That(campaign.Credits, Is.EqualTo(afterMutation ? 100 : 0));
			CampaignState restored = CampaignState.Restore(book, RoundTrip(campaign.Capture()));
			await restored.CompleteSettlementAsync();
			Assert.That(restored.Credits, Is.EqualTo(100));
			Assert.That(restored.Rank.TotalExperience, Is.EqualTo(120));
			Assert.That(restored.Contracts.PendingSettlement, Is.Null);
			Assert.ThrowsAsync<InvalidOperationException>(async () => await restored.CompleteSettlementAsync());
			Assert.That(restored.Credits, Is.EqualTo(100));
			Assert.That(restored.Rank.TotalExperience, Is.EqualTo(120));
		}

		[Test]
		public async Task PendingSettlement_AlreadyAppliedRank_IsNotAppliedAgain()
		{
			ContractBook book = ContractFixtures.Chain();
			CampaignState campaign = CampaignState.CreateNew(book);
			DriveToDelivered(campaign.Contracts, "first");
			ContractSettlement receipt = campaign.PrepareSettlement();
			campaign.Rank.Apply(receipt.Rating);
			CampaignState restored = CampaignState.Restore(book, RoundTrip(campaign.Capture()));
			await restored.CompleteSettlementAsync();
			Assert.That(restored.Rank.TotalExperience, Is.EqualTo(120));
			Assert.That(restored.Credits, Is.EqualTo(100));
		}

		[TestCase(0)]
		[TestCase(-1)]
		public async Task PendingSettlement_NonPositiveTotal_DoesNotGrantCredits(long total)
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());
			DriveToDelivered(campaign.Contracts, "first");
			campaign.PrepareSettlement();
			campaign.Contracts.PendingSettlement.Total = total;

			await campaign.CompleteSettlementAsync();

			Assert.That(campaign.Credits, Is.Zero);
			Assert.That(campaign.Rank.TotalExperience, Is.EqualTo(120));
			Assert.That(campaign.Contracts.PendingSettlement, Is.Null);
		}

		[Test]
		public void PendingSettlement_InvalidSaveIsRejected_AndCaptureIsDetached()
		{
			ContractBook book = ContractFixtures.Chain();
			CampaignState campaign = CampaignState.CreateNew(book);
			DriveToDelivered(campaign.Contracts, "first");
			campaign.PrepareSettlement();
			CampaignSave save = campaign.Capture();
			save.PendingSettlement.Total = -1;
			Assert.That(campaign.Contracts.PendingSettlement.Total, Is.EqualTo(100));
			Assert.Throws<InvalidSaveDataException>(() => CampaignState.Restore(book, save));
		}

		private sealed class FailingCreditHandler : IRewardHandler
		{
			private readonly IRewardHandler _inner;
			private readonly bool _afterMutation;

			public FailingCreditHandler(IRewardHandler inner, bool afterMutation)
			{
				_inner = inner;
				_afterMutation = afterMutation;
			}

			public RewardTypeId Type => _inner.Type;

			public RewardValidationResult Validate(IRewardEntry entry) => _inner.Validate(entry);

			public async Awaitable<RewardApplyResult> ApplyAsync(RewardGrantContext context, IRewardEntry entry, CancellationToken cancellationToken)
			{
				if (_afterMutation)
				{
					await _inner.ApplyAsync(context, entry, cancellationToken);
				}
				return RewardApplyResult.Rejected("Injected reward failure.");
			}
		}

		private static async Task Complete(CampaignState campaign, string id)
		{
			DriveToDelivered(campaign.Contracts, id);
			await campaign.Contracts.SettleAsync();
		}
	}
}
#endif
