#if UNITY_INCLUDE_TESTS
using NightCourier.App;
using NightCourier.Contracts;
using NUnit.Framework;

namespace NightCourier.Tests
{
	public sealed class ObjectiveHintsTests
	{
		private static ContractRun Accepted()
		{
			CampaignState campaign = CampaignState.CreateNew(ContractFixtures.Chain());

			return campaign.Contracts.Accept("first");
		}

		[Test]
		public void Depot_WithoutContract_AsksForTerminal()
		{
			Assert.That(ObjectiveHints.ForDepot(null, false), Is.EqualTo(ObjectiveHints.AcceptContract));
		}

		[Test]
		public void Depot_AcceptedContract_AsksForPackagePickup()
		{
			ContractRun run = Accepted();

			Assert.That(ObjectiveHints.ForDepot(run, false), Is.EqualTo(ObjectiveHints.PickUpFromConveyor));
			Assert.That(ObjectiveHints.ForDepot(run, true), Is.EqualTo(ObjectiveHints.PickUpFromConveyor));
		}

		[Test]
		public void Depot_CollectedPackage_AsksForVanOnlyWhileCarrying()
		{
			ContractRun run = Accepted();
			run.CollectPackage();

			Assert.That(ObjectiveHints.ForDepot(run, true), Is.EqualTo(ObjectiveHints.LoadVan));
			Assert.That(ObjectiveHints.ForDepot(run, false), Is.EqualTo(ObjectiveHints.PickUpFromConveyor));
		}

		[Test]
		public void Depot_AfterDeparture_HasNoObjective()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			Assert.That(ObjectiveHints.ForDepot(run, false), Is.Null);
		}

		[Test]
		public void Location_FollowsCarryState()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			Assert.That(ObjectiveHints.ForLocation(run, new WarehouseProgress(false, false, false)), Is.EqualTo(ObjectiveHints.PickUpPackage));
			Assert.That(ObjectiveHints.ForLocation(run, new WarehouseProgress(false, true, true)), Is.EqualTo(ObjectiveHints.PickUpPackage));
			Assert.That(ObjectiveHints.ForLocation(run, new WarehouseProgress(true, true, true)), Is.EqualTo(ObjectiveHints.DeliverPackage));

			run.Deliver();

			Assert.That(ObjectiveHints.ForLocation(run, new WarehouseProgress(false, true, true)), Is.Null);
			Assert.That(ObjectiveHints.ForLocation(null, new WarehouseProgress(false, false, false)), Is.Null);
		}

		[Test]
		public void Location_CarriedPackage_AsksForPowerThenScan()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			Assert.That(ObjectiveHints.ForLocation(run, new WarehouseProgress(true, false, false)), Is.EqualTo(ObjectiveHints.RestorePower));
			Assert.That(ObjectiveHints.ForLocation(run, new WarehouseProgress(true, true, false)), Is.EqualTo(ObjectiveHints.ScanPackage));
			Assert.That(ObjectiveHints.ForLocation(run, new WarehouseProgress(true, true, true)), Is.EqualTo(ObjectiveHints.DeliverPackage));
		}

		[Test]
		public void Location_Blackout_AsksForBatteryBeforeAnythingElse()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			WarehouseProgress needsBattery = new(true, false, false, batteryRequired: true);
			WarehouseProgress holdingBattery = new(false, false, false, batteryRequired: true, carryingBattery: true);
			WarehouseProgress inserted = new(true, false, false, batteryRequired: true, batteryInserted: true);

			Assert.That(ObjectiveHints.ForLocation(run, needsBattery), Is.EqualTo(ObjectiveHints.FindBattery));
			Assert.That(ObjectiveHints.ForLocation(run, holdingBattery), Is.EqualTo(ObjectiveHints.InsertBattery));
			Assert.That(ObjectiveHints.ForLocation(run, inserted), Is.EqualTo(ObjectiveHints.RestorePower));
		}
	}
}
#endif
