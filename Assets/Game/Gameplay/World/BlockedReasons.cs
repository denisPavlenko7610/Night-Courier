namespace NightCourier.World
{
	public static class BlockedReasons
	{
		public const string DoorNoPower = "blocked.door_power";
		public const string DoorNotScanned = "blocked.door_scan";
		public const string LiftNoPower = "blocked.lift_power";
		public const string BreakerNeedsBattery = "blocked.breaker_battery";
		public const string GeneratorNeedsBattery = "blocked.generator_battery";
		public const string BarricadeIntact = "blocked.barricade";

		public static readonly string[] All =
		{
			DoorNoPower, DoorNotScanned, LiftNoPower, BreakerNeedsBattery, GeneratorNeedsBattery, BarricadeIntact
		};
	}
}
