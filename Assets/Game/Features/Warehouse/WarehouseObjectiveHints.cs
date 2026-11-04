namespace NightCourier.Contracts
{
	public static class WarehouseObjectiveHints
	{
		public const string FindBattery = "objective.find_battery";
		public const string InsertBattery = "objective.insert_battery";
		public const string ParkPackage = "objective.park_package";
		public const string PickUpPackage = "objective.pick_up_package";
		public const string RestorePower = "objective.restore_power";
		public const string ScanPackage = "objective.scan_package";
		public const string Scanning = "objective.scanning";
		public const string PickUpScanned = "objective.pick_up_scanned";
		public const string DeliverPackage = "objective.deliver_package";

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
