using System;
using System.Collections.Generic;
using NightCourier.Economy;

namespace NightCourier.Contracts
{
	public readonly struct ContractOutcome
	{
		public ContractOutcome(
			int violationCount,
			double transitSeconds,
			bool wasOverdue,
			int deathCount = 0,
			int shotsFired = 0,
			int propertyDamageCount = 0,
			double cargoIntegrity = 1d)
		{
			ViolationCount = violationCount;
			TransitSeconds = transitSeconds;
			WasOverdue = wasOverdue;
			DeathCount = deathCount;
			ShotsFired = shotsFired;
			PropertyDamageCount = propertyDamageCount;
			CargoIntegrity = cargoIntegrity;
		}

		public int PropertyDamageCount { get; }

		public double CargoIntegrity { get; }

		public int DeathCount { get; }

		public int ShotsFired { get; }

		public int ViolationCount { get; }

		public double TransitSeconds { get; }

		public bool WasOverdue { get; }
	}

	public enum SettlementLineKind
	{
		BasePay = 0,
		Bonus = 1,
		Deduction = 2
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
		public ContractSettlement(string contractId, IReadOnlyList<SettlementLine> lines, ShiftRating rating = default)
		{
			ContractId = contractId;
			Lines = lines;
			Rating = rating;

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

		public ShiftRating Rating { get; }
	}

	public static class SettlementCalculator
	{
		public const string BasePayLabelKey = "result.base_pay";
		public const string AmmoExpenseLabelKey = "result.ammo_expense";
		public const string PropertyDamageLabelKey = "result.property_damage";
		public const string DeathPenaltyLabelKey = "result.death_penalty";

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

			AddDeduction(lines, AmmoExpenseLabelKey, outcome.ShotsFired, EconomyRules.AmmoCostPerShot);
			AddDeduction(lines, PropertyDamageLabelKey, outcome.PropertyDamageCount, EconomyRules.PropertyDamageFine);
			AddDeduction(lines, DeathPenaltyLabelKey, outcome.DeathCount, EconomyRules.DeathPenalty);

			return new ContractSettlement(spec.Id, lines.AsReadOnly(), ShiftRatingCalculator.Calculate(spec, outcome));
		}

		private static void AddDeduction(List<SettlementLine> lines, string labelKey, int count, long unitCost)
		{
			if (count > 0)
			{
				lines.Add(new SettlementLine(SettlementLineKind.Deduction, labelKey, -count * unitCost));
			}
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
