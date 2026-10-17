using System;
using NightCourier.Contracts;

namespace NightCourier.Packages
{
	public sealed class DeliveryDeadlineRule : PackageRule
	{
		private ContractRun _run;

		public override PackageRuleKind Kind => PackageRuleKind.DeliveryDeadline;

		protected override void OnActivated(PackageRuleContext context)
		{
			Unsubscribe();

			_run = context.Run ?? throw new ArgumentException("A contract run is required.", nameof(context));
			_run.DeadlineExpired += OnDeadlineExpired;
		}

		private void OnDestroy()
		{
			Unsubscribe();
		}

		private void Unsubscribe()
		{
			if (_run != null)
			{
				_run.DeadlineExpired -= OnDeadlineExpired;
				_run = null;
			}
		}

		private void OnDeadlineExpired()
		{
			Violate();
		}
	}
}
