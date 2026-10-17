#if UNITY_INCLUDE_TESTS
using System;
using NightCourier.Contracts;

namespace NightCourier.Tests
{
	internal static class ContractFixtures
	{
		public static ContractSpec Spec(
			string id,
			int reward = 100,
			double timeLimit = 0d,
			string[] prerequisites = null,
			ContractObjectiveSpec[] objectives = null,
			bool repeatable = false
		)
		{
			return new ContractSpec(
				id,
				id + ".title",
				id + ".description",
				ContractType.Deliver,
				"warehouse",
				LocationState.Normal,
				ContractRisk.Low,
				reward,
				timeLimit,
				prerequisites ?? Array.Empty<string>(),
				repeatable,
				objectives ?? Array.Empty<ContractObjectiveSpec>());
		}

		public static ContractObjectiveSpec NoViolations(int bonus = 50)
		{
			return new ContractObjectiveSpec("no-violations", "objective.no_violations", ContractObjectiveKind.NoViolations, 0d, bonus);
		}

		public static ContractObjectiveSpec UnderTime(double seconds, int bonus = 25)
		{
			return new ContractObjectiveSpec("under-time", "objective.on_time", ContractObjectiveKind.UnderTime, seconds, bonus);
		}

		public static ContractBook Chain()
		{
			return new ContractBook(new[]
			{
				Spec("first", 100),
				Spec("second", 200, prerequisites: new[] { "first" })
			});
		}
	}
}
#endif
