using System;

namespace NightCourier.Contracts
{
	public enum EscalationEffect
	{
		PackageUnstable = 0,
		FailContract = 1
	}

	public readonly struct EscalationStep : IEquatable<EscalationStep>
	{
		public EscalationStep(int atViolation, EscalationEffect effect)
		{
			if (atViolation < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(atViolation), atViolation, "An escalation step needs a violation count of at least 1.");
			}

			AtViolation = atViolation;
			Effect = effect;
		}

		public int AtViolation { get; }

		public EscalationEffect Effect { get; }

		public bool Equals(EscalationStep other)
		{
			return AtViolation == other.AtViolation && Effect == other.Effect;
		}

		public override bool Equals(object obj)
		{
			return obj is EscalationStep other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(AtViolation, Effect);
		}
	}
}
