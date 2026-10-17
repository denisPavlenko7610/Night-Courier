using NightCourier.Contracts;
using UnityEngine;

namespace NightCourier.Packages
{
	public sealed class DoNotDropRule : PackageRule
	{
		[SerializeField] private PackageImpact _impact;
		[SerializeField, Min(0f)] private float _hardImpactSpeed = 8f;

		public override PackageRuleKind Kind => PackageRuleKind.DoNotDrop;

		public static bool IsViolation(PackageImpactInfo info, float hardImpactSpeed)
		{
			return !info.OnSafeSurface && (info.AfterRelease || info.Speed >= hardImpactSpeed);
		}

		protected override void OnActivated(PackageRuleContext context)
		{
			_impact.Impacted -= OnImpacted;
			_impact.Impacted += OnImpacted;
		}

		private void OnDestroy()
		{
			if (_impact != null)
			{
				_impact.Impacted -= OnImpacted;
			}
		}

		private void OnImpacted(PackageImpactInfo info)
		{
			if (enabled && IsViolation(info, _hardImpactSpeed))
			{
				Violate();
			}
		}
	}
}
