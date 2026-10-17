#if UNITY_INCLUDE_TESTS
using System;
using System.Threading.Tasks;
using NightCourier.App;
using NightCourier.Contracts;
using NUnit.Framework;

namespace NightCourier.Tests
{
	public sealed class ContractBoardTests
	{
		private static async Task<ContractSettlement> Complete(ContractBoard board, string id, double transitSeconds = 1d)
		{
			ContractRun run = board.Accept(id);

			Assert.That(run.CollectPackage(), Is.True);
			Assert.That(run.Depart(), Is.True);
			run.Tick(transitSeconds);
			Assert.That(run.Deliver(), Is.True);

			return await board.SettleAsync();
		}

		[Test]
		public void Available_StartsWithContractsThatHaveNoPrerequisites()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());

			ContractSpec[] available = new ContractSpec[campaign.Contracts.GetAvailable().Count];
			for (int index = 0; index < available.Length; index++)
			{
				available[index] = campaign.Contracts.GetAvailable()[index];
			}

			Assert.That(available.Length, Is.EqualTo(1));
			Assert.That(available[0].Id, Is.EqualTo("first"));
		}

		[Test]
		public void Accept_RejectsUnavailableAndSecondContracts()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());

			Assert.Throws<InvalidOperationException>(() => campaign.Contracts.Accept("second"));

			ContractRun run = campaign.Contracts.Accept("first");

			Assert.That(run.Phase, Is.EqualTo(ContractPhase.Accepted));
			Assert.That(campaign.Contracts.GetAvailable().Count, Is.EqualTo(0));
			Assert.Throws<InvalidOperationException>(() => campaign.Contracts.Accept("first"));
		}

		[Test]
		public async Task FullLifecycle_GrantsRewardAndUnlocksDependents()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());

			ContractSettlement settlement = await Complete(campaign.Contracts, "first");

			Assert.That(settlement.Total, Is.EqualTo(100));
			Assert.That(campaign.Credits, Is.EqualTo(100));
			Assert.That(campaign.Contracts.ActiveRun, Is.Null);
			Assert.That(campaign.Contracts.CompletedCount, Is.EqualTo(1));
			Assert.That(campaign.Contracts.GetAvailable()[0].Id, Is.EqualTo("second"));
		}

		[Test]
		public void Phases_CannotBeSkipped()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());
			ContractRun run = campaign.Contracts.Accept("first");

			Assert.That(run.Depart(), Is.False);
			Assert.That(run.Deliver(), Is.False);
			Assert.That(run.CollectPackage(), Is.True);
			Assert.That(run.CollectPackage(), Is.False);
			Assert.That(run.Phase, Is.EqualTo(ContractPhase.PackageCollected));
		}

		[Test]
		public void Settle_WithoutDelivery_Throws()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());

			Assert.ThrowsAsync<InvalidOperationException>(async () => await campaign.Contracts.SettleAsync());

			campaign.Contracts.Accept("first");

			Assert.ThrowsAsync<InvalidOperationException>(async () => await campaign.Contracts.SettleAsync());
			Assert.That(campaign.Credits, Is.EqualTo(0));
		}

		[Test]
		public async Task Settle_Twice_PaysOnce()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());

			await Complete(campaign.Contracts, "first");

			Assert.ThrowsAsync<InvalidOperationException>(async () => await campaign.Contracts.SettleAsync());
			Assert.That(campaign.Credits, Is.EqualTo(100));
		}

		[Test]
		public void Fail_ReturnsContractToAvailableWithoutReward()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());
			ContractRun run = campaign.Contracts.Accept("first");
			run.CollectPackage();

			Assert.That(campaign.Contracts.Fail(), Is.True);

			Assert.That(run.Phase, Is.EqualTo(ContractPhase.Failed));
			Assert.That(campaign.Contracts.ActiveRun, Is.Null);
			Assert.That(campaign.Credits, Is.EqualTo(0));
			Assert.That(campaign.Contracts.GetAvailable()[0].Id, Is.EqualTo("first"));
			Assert.That(campaign.Contracts.Fail(), Is.False);
		}

		[Test]
		public async Task Deadline_ExpiresOnlyWhileInTransit_AndSkipsTimeBonus()
		{
			ContractBook book = new(new[]
			{
				ContractFixtures.Spec("timed", 100, timeLimit: 10d, objectives: new[] { ContractFixtures.UnderTime(20d, 30) })
			});
			CampaignState campaign = CampaignState.CreateNew(book);
			ContractRun run = campaign.Contracts.Accept("timed");

			run.Tick(100d);
			Assert.That(run.IsOverdue, Is.False);

			run.CollectPackage();
			run.Depart();
			run.Tick(4d);
			Assert.That(run.RemainingSeconds, Is.EqualTo(6d).Within(0.001));

			run.Tick(7d);
			Assert.That(run.IsOverdue, Is.True);

			run.Deliver();
			ContractSettlement settlement = await campaign.Contracts.SettleAsync();

			Assert.That(settlement.Total, Is.EqualTo(100));
		}

		[Test]
		public async Task Violations_RemoveTheNoViolationsBonus()
		{
			ContractBook book = new(new[]
			{
				ContractFixtures.Spec("careful", 100, objectives: new[] { ContractFixtures.NoViolations(50) })
			});
			CampaignState campaign = CampaignState.CreateNew(book);
			ContractRun run = campaign.Contracts.Accept("careful");
			run.CollectPackage();
			run.Depart();
			run.RegisterViolation();
			run.Deliver();

			ContractSettlement settlement = await campaign.Contracts.SettleAsync();

			Assert.That(settlement.Total, Is.EqualTo(100));
		}

		[Test]
		public async Task RepeatableContract_PaysEachCompletionOnce()
		{
			ContractBook book = new(new[] { ContractFixtures.Spec("loop", 80, repeatable: true) });
			CampaignState campaign = CampaignState.CreateNew(book);

			await Complete(campaign.Contracts, "loop");
			await Complete(campaign.Contracts, "loop");

			Assert.That(campaign.Credits, Is.EqualTo(160));
		}
	}
}
#endif
