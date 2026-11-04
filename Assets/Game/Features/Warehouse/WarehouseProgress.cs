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
}
