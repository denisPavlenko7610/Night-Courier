using System;
using NightCourier.Contracts;

namespace NightCourier.Packages
{
	public static class PackageRuleKeys
	{
		public static string NameKey(PackageRuleKind kind)
		{
			return kind switch
			{
				PackageRuleKind.DoNotDrop => "rule.do_not_drop",
				PackageRuleKind.KeepInLight => "rule.keep_in_light",
				PackageRuleKind.DeliveryDeadline => "rule.delivery_deadline",
				_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown package rule.")
			};
		}

		public static string HintKey(PackageRuleKind kind)
		{
			return NameKey(kind) + ".hint";
		}

		public static string AdviceKey(PackageRuleKind kind)
		{
			return NameKey(kind) + ".advice";
		}
	}

	public static class PackageDamageKeys
	{
		public const string ImpactType = "impact";
		public const string ElectricType = "electric";

		public static string NoticeKey(string damageType)
		{
			return damageType switch
			{
				ImpactType => "notice.package_damaged.impact",
				ElectricType => "notice.package_damaged.electric",
				_ => "notice.package_damaged"
			};
		}
	}
}
