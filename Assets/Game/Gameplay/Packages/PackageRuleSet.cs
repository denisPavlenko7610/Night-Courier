using System;
using System.Collections.Generic;
using NightCourier.Contracts;
using UnityEngine;

namespace NightCourier.Packages
{
	public sealed class PackageRuleSet : MonoBehaviour
	{
		private readonly List<PackageRuleKind> _active = new();

		public IReadOnlyList<PackageRuleKind> Active => _active;

		public event Action<PackageRuleKind> Violated;

		public void Configure(IReadOnlyList<PackageRuleKind> kinds, PackageRuleContext context)
		{
			if (kinds == null)
			{
				throw new ArgumentNullException(nameof(kinds));
			}

			if (_active.Count > 0)
			{
				throw new InvalidOperationException("The package rules are already configured.");
			}

			PackageRule[] available = GetComponents<PackageRule>();

			foreach (PackageRuleKind kind in kinds)
			{
				PackageRule rule = Array.Find(available, candidate => candidate.Kind == kind)
					?? throw new InvalidOperationException($"The package has no component for rule '{kind}'.");

				rule.Violated += () => Violated?.Invoke(kind);
				rule.Activate(context);
				_active.Add(kind);
			}
		}
	}
}
