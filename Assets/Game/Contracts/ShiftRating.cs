using System;

namespace NightCourier.Contracts
{
	public enum ShiftGrade
	{
		F = 0,
		D = 1,
		C = 2,
		B = 3,
		A = 4,
		S = 5
	}

	public readonly struct ShiftRating
	{
		public ShiftRating(
			ShiftGrade? deliveryTime,
			ShiftGrade cargoCondition,
			ShiftGrade protocolCompliance,
			ShiftGrade propertyDamage)
		{
			DeliveryTime = deliveryTime;
			CargoCondition = cargoCondition;
			ProtocolCompliance = protocolCompliance;
			PropertyDamage = propertyDamage;

			int sum = (int)cargoCondition + (int)protocolCompliance + (int)propertyDamage;
			int count = 3;

			if (deliveryTime.HasValue)
			{
				sum += (int)deliveryTime.Value;
				count++;
			}

			Overall = (ShiftGrade)Math.Min((int)ShiftGrade.S, (2 * sum + count) / (2 * count));
		}

		public ShiftGrade? DeliveryTime { get; }

		public ShiftGrade CargoCondition { get; }

		public ShiftGrade ProtocolCompliance { get; }

		public ShiftGrade PropertyDamage { get; }

		public ShiftGrade Overall { get; }
	}

	public static class ShiftRatingCalculator
	{
		private const double SGradeCargo = 0.99d;
		private const double AGradeCargo = 0.85d;
		private const double BGradeCargo = 0.65d;
		private const double CGradeCargo = 0.4d;

		private const double SGradeTime = 0.4d;
		private const double AGradeTime = 0.6d;
		private const double BGradeTime = 0.8d;

		private static readonly ShiftGrade[] ProtocolByViolations =
		{
			ShiftGrade.S, ShiftGrade.B, ShiftGrade.C, ShiftGrade.D, ShiftGrade.F
		};

		private static readonly ShiftGrade[] PropertyByEvents =
		{
			ShiftGrade.S, ShiftGrade.B, ShiftGrade.C, ShiftGrade.F
		};

		public static ShiftRating Calculate(ContractSpec spec, ContractOutcome outcome)
		{
			if (spec == null)
			{
				throw new ArgumentNullException(nameof(spec));
			}

			int packageViolations = Math.Max(0, outcome.ViolationCount - outcome.PropertyDamageCount);

			return new ShiftRating(
				DeliveryGrade(spec, outcome),
				CargoGrade(outcome.CargoIntegrity),
				ByCount(packageViolations, ProtocolByViolations),
				ByCount(outcome.PropertyDamageCount, PropertyByEvents));
		}

		private static ShiftGrade? DeliveryGrade(ContractSpec spec, ContractOutcome outcome)
		{
			if (!spec.HasTimeLimit)
			{
				return null;
			}

			if (outcome.WasOverdue)
			{
				return ShiftGrade.F;
			}

			double used = outcome.TransitSeconds / spec.TimeLimitSeconds;

			if (used <= SGradeTime)
			{
				return ShiftGrade.S;
			}

			if (used <= AGradeTime)
			{
				return ShiftGrade.A;
			}

			return used <= BGradeTime ? ShiftGrade.B : ShiftGrade.C;
		}

		private static ShiftGrade CargoGrade(double integrity)
		{
			if (integrity >= SGradeCargo)
			{
				return ShiftGrade.S;
			}

			if (integrity >= AGradeCargo)
			{
				return ShiftGrade.A;
			}

			if (integrity >= BGradeCargo)
			{
				return ShiftGrade.B;
			}

			return integrity >= CGradeCargo ? ShiftGrade.C : ShiftGrade.D;
		}

		private static ShiftGrade ByCount(int count, ShiftGrade[] table)
		{
			return table[Math.Min(Math.Max(count, 0), table.Length - 1)];
		}
	}
}
