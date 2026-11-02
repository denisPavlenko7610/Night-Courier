using System;

namespace NightCourier.Contracts
{
	public static class ContractKeys
	{
		public static string RiskKey(ContractRisk risk)
		{
			return risk switch
			{
				ContractRisk.Low => "risk.low",
				ContractRisk.Medium => "risk.medium",
				ContractRisk.High => "risk.high",
				_ => throw new ArgumentOutOfRangeException(nameof(risk), risk, "Unknown contract risk.")
			};
		}

		public static string StateKey(LocationState state)
		{
			return state switch
			{
				LocationState.Normal => "location_state.normal",
				LocationState.Blackout => "location_state.blackout",
				_ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown location state.")
			};
		}

		public static string ThreatKey(EncounterKind encounter)
		{
			return encounter switch
			{
				EncounterKind.Crawlers => "threat.crawlers",
				EncounterKind.SecurityDrone => "threat.security_drone",
				_ => throw new ArgumentOutOfRangeException(nameof(encounter), encounter, "Unknown encounter.")
			};
		}
	}
}
