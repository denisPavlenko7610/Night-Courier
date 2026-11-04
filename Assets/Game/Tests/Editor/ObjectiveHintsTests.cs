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
			Assert.That(DepotObjectiveHints.ForDepot(null, false), Is.EqualTo(DepotObjectiveHints.AcceptContract));
		}

		[Test]
		public void Depot_AcceptedContract_AsksForPackagePickup()
		{
			ContractRun run = Accepted();

			Assert.That(DepotObjectiveHints.ForDepot(run, false), Is.EqualTo(DepotObjectiveHints.PickUpFromConveyor));
			Assert.That(DepotObjectiveHints.ForDepot(run, true), Is.EqualTo(DepotObjectiveHints.PickUpFromConveyor));
		}

		[Test]
		public void Depot_CollectedPackage_AsksForVanOnlyWhileCarrying()
		{
			ContractRun run = Accepted();
			run.CollectPackage();

			Assert.That(DepotObjectiveHints.ForDepot(run, true), Is.EqualTo(DepotObjectiveHints.LoadVan));
			Assert.That(DepotObjectiveHints.ForDepot(run, false), Is.EqualTo(DepotObjectiveHints.PickUpFromConveyor));
		}

		[Test]
		public void Depot_AfterDeparture_HasNoObjective()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			Assert.That(DepotObjectiveHints.ForDepot(run, false), Is.Null);
		}

		[Test]
		public void Location_FollowsCarryState()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			Assert.That(WarehouseObjectiveHints.ForLocation(run, new WarehouseProgress(false, false, false)), Is.EqualTo(WarehouseObjectiveHints.PickUpPackage));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, new WarehouseProgress(false, true, false)), Is.EqualTo(WarehouseObjectiveHints.PickUpPackage));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, new WarehouseProgress(false, true, true)), Is.EqualTo(WarehouseObjectiveHints.PickUpScanned));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, new WarehouseProgress(true, true, true)), Is.EqualTo(WarehouseObjectiveHints.DeliverPackage));

			run.Deliver();

			Assert.That(WarehouseObjectiveHints.ForLocation(run, new WarehouseProgress(false, true, true)), Is.Null);
			Assert.That(WarehouseObjectiveHints.ForLocation(null, new WarehouseProgress(false, false, false)), Is.Null);
		}

		[Test]
		public void Location_CarriedPackage_AsksForPowerThenScan()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			Assert.That(WarehouseObjectiveHints.ForLocation(run, new WarehouseProgress(true, false, false)), Is.EqualTo(WarehouseObjectiveHints.RestorePower));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, new WarehouseProgress(true, true, false)), Is.EqualTo(WarehouseObjectiveHints.ScanPackage));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, new WarehouseProgress(true, true, true)), Is.EqualTo(WarehouseObjectiveHints.DeliverPackage));
		}

		[Test]
		public void Location_PackageOnTheScanner_ExplainsTheScanInsteadOfAskingForPickup()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			WarehouseProgress scanning = new(false, true, false, scanInProgress: true);
			WarehouseProgress interrupted = new(false, true, false);
			WarehouseProgress carriedWhileScanFlagIsStale = new(true, true, false, scanInProgress: true);

			Assert.That(WarehouseObjectiveHints.ForLocation(run, scanning), Is.EqualTo(WarehouseObjectiveHints.Scanning));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, interrupted), Is.EqualTo(WarehouseObjectiveHints.PickUpPackage));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, carriedWhileScanFlagIsStale), Is.EqualTo(WarehouseObjectiveHints.ScanPackage));
		}

		[Test]
		public void Location_Blackout_AsksForBatteryBeforeAnythingElse()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			WarehouseProgress needsBattery = new(false, false, false, batteryRequired: true);
			WarehouseProgress holdingBattery = new(false, false, false, batteryRequired: true, carryingBattery: true);
			WarehouseProgress inserted = new(true, false, false, batteryRequired: true, batteryInserted: true);

			Assert.That(WarehouseObjectiveHints.ForLocation(run, needsBattery), Is.EqualTo(WarehouseObjectiveHints.FindBattery));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, holdingBattery), Is.EqualTo(WarehouseObjectiveHints.InsertBattery));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, inserted), Is.EqualTo(WarehouseObjectiveHints.RestorePower));
		}

		[Test]
		public void Location_Blackout_CarriedPackage_AsksToParkItBeforeTheBattery()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			WarehouseProgress carrying = new(true, false, false, batteryRequired: true);
			WarehouseProgress parked = new(false, false, false, batteryRequired: true);

			Assert.That(WarehouseObjectiveHints.ForLocation(run, carrying), Is.EqualTo(WarehouseObjectiveHints.ParkPackage));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, parked), Is.EqualTo(WarehouseObjectiveHints.FindBattery));
		}

		[Test]
		public void Location_PackageOnTheScannerWithoutPower_AsksForPowerInsteadOfPickup()
		{
			ContractRun run = Accepted();
			run.CollectPackage();
			run.Depart();

			WarehouseProgress unpowered = new(false, false, false, packageOnScanner: true);
			WarehouseProgress powered = new(false, true, false, packageOnScanner: true);
			WarehouseProgress elsewhere = new(false, false, false);

			Assert.That(WarehouseObjectiveHints.ForLocation(run, unpowered), Is.EqualTo(WarehouseObjectiveHints.RestorePower));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, powered), Is.EqualTo(WarehouseObjectiveHints.PickUpPackage));
			Assert.That(WarehouseObjectiveHints.ForLocation(run, elsewhere), Is.EqualTo(WarehouseObjectiveHints.PickUpPackage));
		}
	}
}
#endif
