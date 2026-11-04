using System;
using NightCourier.Contracts;
using UnityEngine;

namespace NightCourier.Packages
{
	public readonly struct PackageRuleContext
	{
		public PackageRuleContext(ContractRun run, ILightField lights)
		{
			Run = run;
			Lights = lights;
		}

		public ContractRun Run { get; }

		public ILightField Lights { get; }
	}

	public abstract class PackageRule : MonoBehaviour
	{
		public abstract PackageRuleKind Kind { get; }

		public event Action Violated;

		public void Activate(PackageRuleContext context)
		{
			OnActivated(context);
			enabled = true;
		}

		protected abstract void OnActivated(PackageRuleContext context);

		protected void Violate()
		{
			Violated?.Invoke();
		}
	}
}
