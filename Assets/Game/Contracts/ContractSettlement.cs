using System;
using System.Collections.Generic;

namespace NightCourier.Contracts
{
	public readonly struct ContractOutcome
	{
		public ContractOutcome(int violationCount, double transitSeconds, bool wasOverdue, int deathCount = 0)
		{
			ViolationCount = violationCount;
			TransitSeconds = transitSeconds;
			WasOverdue = wasOverdue;
			DeathCount = deathCount;
		}

		public int DeathCount { get; }

		public int ViolationCount { get; }

		public double TransitSeconds { get; }

		public bool WasOverdue { get; }
	}

	public enum SettlementLineKind
	{
		BasePay = 0,
		Bonus = 1
	}

	public readonly struct SettlementLine
	{
		public SettlementLine(SettlementLineKind kind, string labelKey, long amount)
		{
			Kind = kind;
			LabelKey = labelKey;
			Amount = amount;
		}

		public SettlementLineKind Kind { get; }

		public string LabelKey { get; }

		public long Amount { get; }
	}

	public sealed class ContractSettlement
	{
		public ContractSettlement(string contractId, IReadOnlyList<SettlementLine> lines)
		{
			ContractId = contractId;
			Lines = lines;

			long sum = 0;

			foreach (SettlementLine line in lines)
			{
				sum += line.Amount;
			}

			Total = Math.Max(0, sum);
		}

		public string ContractId { get; }

		public IReadOnlyList<SettlementLine> Lines { get; }

		public long Total { get; }
	}

	public static class SettlementCalculator
	{
		public const string BasePayLabelKey = "result.base_pay";

		public static ContractSettlement Calculate(ContractSpec spec, ContractOutcome outcome)
		{
			if (spec == null)
			{
				throw new ArgumentNullException(nameof(spec));
			}

			List<SettlementLine> lines = new() { new SettlementLine(SettlementLineKind.BasePay, BasePayLabelKey, spec.BaseReward) };

			foreach (ContractObjectiveSpec objective in spec.Objectives)
			{
				if (IsMet(objective, outcome))
				{
					lines.Add(new SettlementLine(SettlementLineKind.Bonus, objective.LabelKey, objective.BonusCredits));
				}
			}

			return new ContractSettlement(spec.Id, lines.AsReadOnly());
		}

		private static bool IsMet(ContractObjectiveSpec objective, ContractOutcome outcome)
		{
			return objective.Kind switch
			{
				ContractObjectiveKind.NoViolations => outcome.ViolationCount == 0,
				ContractObjectiveKind.UnderTime => !outcome.WasOverdue && outcome.TransitSeconds <= objective.ThresholdSeconds,
				_ => false
			};
		}
	}
}
