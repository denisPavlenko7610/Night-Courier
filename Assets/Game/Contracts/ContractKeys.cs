using System;

namespace NightCourier.Contracts
{
	public static class ContractKeys
	{
		public const string TitleSuffix = ".title";

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

		public static string OrderKey(string titleKey)
		{
			return Companion(titleKey, ".order");
		}

		public static string NotesKey(string titleKey)
		{
			return Companion(titleKey, ".notes");
		}

		public static string MemoKey(string titleKey)
		{
			return Companion(titleKey, ".memo");
		}

		public static string LocationKey(string sceneId)
		{
			if (string.IsNullOrEmpty(sceneId))
			{
				throw new ArgumentException("A scene id is required.", nameof(sceneId));
			}

			return "location." + sceneId;
		}

		private static string Companion(string titleKey, string suffix)
		{
			if (string.IsNullOrEmpty(titleKey) || !titleKey.EndsWith(TitleSuffix, StringComparison.Ordinal))
			{
				throw new ArgumentException("A contract title key must end with '" + TitleSuffix + "'.", nameof(titleKey));
			}

			return titleKey.Substring(0, titleKey.Length - TitleSuffix.Length) + suffix;
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
