namespace NightCourier.Contracts
{
	public static class DepotObjectiveHints
	{
		public const string AcceptContract = "objective.accept_contract";
		public const string PickUpFromConveyor = "objective.pick_up_from_conveyor";
		public const string LoadVan = "objective.load_van";

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
	}
}
