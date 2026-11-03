namespace NightCourier.Contracts
{
	public readonly struct WarehouseProgress
	{
		public WarehouseProgress(
			bool carryingPackage,
			bool powerOn,
			bool cargoScanned,
			bool batteryRequired = false,
			bool batteryInserted = false,
			bool carryingBattery = false,
			bool scanInProgress = false,
			bool packageOnScanner = false
		)
		{
			CarryingPackage = carryingPackage;
			PowerOn = powerOn;
			CargoScanned = cargoScanned;
			BatteryRequired = batteryRequired;
			BatteryInserted = batteryInserted;
			CarryingBattery = carryingBattery;
			ScanInProgress = scanInProgress;
			PackageOnScanner = packageOnScanner;
		}

		public bool PackageOnScanner { get; }

		public bool ScanInProgress { get; }

		public bool CarryingPackage { get; }

		public bool PowerOn { get; }

		public bool CargoScanned { get; }

		public bool BatteryRequired { get; }

		public bool BatteryInserted { get; }

		public bool CarryingBattery { get; }
	}

	public static class ObjectiveHints
	{
		public const string FindBattery = "objective.find_battery";
		public const string InsertBattery = "objective.insert_battery";
		public const string ParkPackage = "objective.park_package";

		public const string AcceptContract = "objective.accept_contract";
		public const string PickUpFromConveyor = "objective.pick_up_from_conveyor";
		public const string LoadVan = "objective.load_van";
		public const string PickUpPackage = "objective.pick_up_package";
		public const string RestorePower = "objective.restore_power";
		public const string ScanPackage = "objective.scan_package";
		public const string Scanning = "objective.scanning";
		public const string PickUpScanned = "objective.pick_up_scanned";
		public const string DeliverPackage = "objective.deliver_package";

		public static string ForDepot(ContractRun run, bool carryingPackage)
		{
			if (run == null)
			{
				return AcceptContract;
			}

			return run.Phase switch
			{
				ContractPhase.Accepted => PickUpFromConveyor,
				ContractPhase.PackageCollected => carryingPackage ? LoadVan : PickUpFromConveyor,
				_ => null
			};
		}

		public static string ForLocation(ContractRun run, WarehouseProgress progress)
		{
			if (run == null || run.Phase != ContractPhase.InTransit)
			{
				return null;
			}

			if (progress.CarryingBattery)
			{
				return InsertBattery;
			}

			if (progress.BatteryRequired && !progress.BatteryInserted)
			{
				return progress.CarryingPackage ? ParkPackage : FindBattery;
			}

			if (!progress.CarryingPackage)
			{
				if (progress.CargoScanned)
				{
					return PickUpScanned;
				}

				if (progress.ScanInProgress)
				{
					return Scanning;
				}

				return progress.PackageOnScanner && !progress.PowerOn ? RestorePower : PickUpPackage;
			}

			if (progress.CargoScanned)
			{
				return DeliverPackage;
			}

			return progress.PowerOn ? ScanPackage : RestorePower;
		}
	}
}
