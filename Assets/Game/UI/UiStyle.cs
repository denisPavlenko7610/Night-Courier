using System;
using System.Globalization;
using NightCourier.Contracts;

namespace NightCourier.UI
{
	public static class UiStyle
	{
		public const string AccentColor = "#F2A640";
		public const string MutedColor = "#8A98A8";
		public const string GoodColor = "#6FD08C";
		public const string WarningColor = "#F2C14E";
		public const string DangerColor = "#EF6B5B";
		public const string CurrencySymbol = "₡";
		public const string BulletSymbol = "•";
		public const string FilledPip = "■";
		public const string EmptyPip = "□";
		public const float GoodIntegrity = 0.7f;
		public const float WarningIntegrity = 0.4f;

		public static string Tint(string text, string color)
		{
			return "<color=" + color + ">" + text + "</color>";
		}

		public static string Caption(string text)
		{
			return Tint(text.ToUpperInvariant(), MutedColor);
		}

		public static string Heading(string text)
		{
			return Tint(text.ToUpperInvariant(), AccentColor);
		}

		public static string Italic(string text)
		{
			return "<i>" + text + "</i>";
		}

		public static string Money(long amount)
		{
			return amount < 0
				? "-" + CurrencySymbol + (-amount).ToString(CultureInfo.InvariantCulture)
				: CurrencySymbol + amount.ToString(CultureInfo.InvariantCulture);
		}

		public static string SignedMoney(long amount)
		{
			return amount < 0 ? Money(amount) : "+" + Money(amount);
		}

		public static string Pips(int filled, int total)
		{
			int clamped = Math.Clamp(filled, 0, Math.Max(0, total));

			return new string(FilledPip[0], clamped) + new string(EmptyPip[0], Math.Max(0, total) - clamped);
		}

		public static string GradeColor(ShiftGrade grade)
		{
			return grade switch
			{
				ShiftGrade.S => GoodColor,
				ShiftGrade.A => GoodColor,
				ShiftGrade.B => WarningColor,
				ShiftGrade.C => WarningColor,
				_ => DangerColor
			};
		}

		public static string RiskColor(ContractRisk risk)
		{
			return risk switch
			{
				ContractRisk.Low => GoodColor,
				ContractRisk.Medium => WarningColor,
				_ => DangerColor
			};
		}

		public static string IntegrityColor(float fraction)
		{
			if (fraction >= GoodIntegrity)
			{
				return GoodColor;
			}

			return fraction >= WarningIntegrity ? WarningColor : DangerColor;
		}
	}
}
