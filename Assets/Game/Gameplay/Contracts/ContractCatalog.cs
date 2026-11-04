using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightCourier.Contracts
{
	[CreateAssetMenu(fileName = "ContractCatalog", menuName = "Night Courier/Contract Catalog")]
	public sealed class ContractCatalog : ScriptableObject
	{
		[SerializeField] private List<ContractDefinition> _contracts = new();

		public ContractBook BuildBook()
		{
			List<ContractSpec> specs = new(_contracts.Count);

			foreach (ContractDefinition definition in _contracts)
			{
				if (definition == null)
				{
					throw new InvalidOperationException($"Contract catalog '{name}' contains an empty entry.");
				}

				try
				{
					specs.Add(definition.ToSpec());
				}
				catch (ArgumentException exception)
				{
					throw new InvalidOperationException($"Contract '{definition.name}' is invalid: {exception.Message}", exception);
				}
			}

			return new ContractBook(specs);
		}
	}
}
