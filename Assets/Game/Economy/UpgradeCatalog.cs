using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightCourier.Economy
{
	[CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "Night Courier/Upgrade Catalog")]
	public sealed class UpgradeCatalog : ScriptableObject
	{
		[SerializeField] private List<UpgradeDefinition> _upgrades = new();

		public UpgradeBook BuildBook()
		{
			List<UpgradeSpec> specs = new(_upgrades.Count);

			foreach (UpgradeDefinition definition in _upgrades)
			{
				if (definition == null)
				{
					throw new InvalidOperationException($"Upgrade catalog '{name}' contains an empty entry.");
				}

				try
				{
					specs.Add(definition.ToSpec());
				}
				catch (ArgumentException exception)
				{
					throw new InvalidOperationException($"Upgrade '{definition.name}' is invalid: {exception.Message}", exception);
				}
			}

			return new UpgradeBook(specs);
		}
	}
}
