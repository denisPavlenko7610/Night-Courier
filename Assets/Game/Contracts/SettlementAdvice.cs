using System.Collections.Generic;
using NightCourier.Packages;

namespace NightCourier.Contracts
{
	public static class SettlementAdvice
	{
		public const string CleanKey = "advice.clean";
		public const string OverdueKey = "advice.overdue";
		public const string CargoKey = "advice.cargo";
		public const string PropertyKey = "advice.property";
		public const string DeathKey = "advice.death";
		public const int MaxLines = 3;
		public const double CargoAdviceBelow = 0.85d;

		public static IReadOnlyList<string> For(ContractOutcome outcome)
		{
			List<string> keys = new();

			if (outcome.WasOverdue)
			{
				keys.Add(OverdueKey);
			}

			foreach (RuleViolationCount violation in outcome.RuleViolations)
			{
				if (violation.Kind == PackageRuleKind.DeliveryDeadline && outcome.WasOverdue)
				{
					continue;
				}

				keys.Add(PackageRuleKeys.AdviceKey(violation.Kind));
			}

			if (outcome.CargoIntegrity < CargoAdviceBelow)
			{
				keys.Add(CargoKey);
			}

			if (outcome.PropertyDamageCount > 0)
			{
				keys.Add(PropertyKey);
			}

			if (outcome.DeathCount > 0)
			{
				keys.Add(DeathKey);
			}

			if (keys.Count == 0)
			{
				keys.Add(CleanKey);
			}

			if (keys.Count > MaxLines)
			{
				keys.RemoveRange(MaxLines, keys.Count - MaxLines);
			}

			return keys.AsReadOnly();
		}
	}
}
