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
	}
}
