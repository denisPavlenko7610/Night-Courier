namespace NightCourier.Contracts
{
	public enum ContractType
	{
		Deliver = 0
	}

	public enum ContractRisk
	{
		Low = 0,
		Medium = 1,
		High = 2
	}

	public enum LocationState
	{
		Normal = 0,
		Blackout = 1
	}

	public enum ContractPhase
	{
		Accepted = 0,
		PackageCollected = 1,
		InTransit = 2,
		Delivered = 3,
		Completed = 4,
		Failed = 5
	}

	public enum ContractObjectiveKind
	{
		NoViolations = 0,
		UnderTime = 1
	}
}
