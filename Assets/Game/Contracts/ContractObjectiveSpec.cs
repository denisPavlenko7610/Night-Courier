using System;

namespace NightCourier.Contracts
{
	public sealed class ContractObjectiveSpec
	{
		public ContractObjectiveSpec(
			string id,
			string labelKey,
			ContractObjectiveKind kind,
			double thresholdSeconds,
			int bonusCredits
		)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("An objective id is required.", nameof(id));
			}

			if (string.IsNullOrWhiteSpace(labelKey))
			{
				throw new ArgumentException("An objective label key is required.", nameof(labelKey));
			}

			if (bonusCredits <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(bonusCredits), bonusCredits, "A bonus must be positive.");
			}

			bool needsThreshold = kind == ContractObjectiveKind.UnderTime;

			if (needsThreshold && (thresholdSeconds <= 0d || double.IsNaN(thresholdSeconds) || double.IsInfinity(thresholdSeconds)))
			{
				throw new ArgumentOutOfRangeException(
					nameof(thresholdSeconds), thresholdSeconds, "A time objective needs a positive finite threshold.");
			}

			Id = id;
			LabelKey = labelKey;
			Kind = kind;
			ThresholdSeconds = needsThreshold ? thresholdSeconds : 0d;
			BonusCredits = bonusCredits;
		}

		public string Id { get; }

		public string LabelKey { get; }

		public ContractObjectiveKind Kind { get; }

		public double ThresholdSeconds { get; }

		public int BonusCredits { get; }
	}
}
