using System;
using System.Collections.Generic;

namespace NightCourier.Contracts
{
	public sealed class ContractBook
	{
		private readonly Dictionary<string, ContractSpec> _byId = new(StringComparer.Ordinal);
		private readonly List<ContractSpec> _ordered = new();

		public ContractBook(IEnumerable<ContractSpec> specs)
		{
			if (specs == null)
			{
				throw new ArgumentNullException(nameof(specs));
			}

			foreach (ContractSpec spec in specs)
			{
				if (spec == null)
				{
					throw new ArgumentException("The contract list contains an empty entry.", nameof(specs));
				}

				if (!_byId.TryAdd(spec.Id, spec))
				{
					throw new ArgumentException($"Contract id '{spec.Id}' is registered more than once.", nameof(specs));
				}

				_ordered.Add(spec);
			}

			foreach (ContractSpec spec in _ordered)
			{
				foreach (string prerequisite in spec.Prerequisites)
				{
					if (!_byId.ContainsKey(prerequisite))
					{
						throw new ArgumentException(
							$"Contract '{spec.Id}' requires unknown contract '{prerequisite}'.", nameof(specs));
					}
				}
			}
		}

		public IReadOnlyList<ContractSpec> All => _ordered;

		public ContractSpec Get(string id)
		{
			return _byId.TryGetValue(id ?? string.Empty, out ContractSpec spec)
				? spec
				: throw new KeyNotFoundException($"Contract '{id}' is not registered.");
		}

		public bool TryGet(string id, out ContractSpec spec)
		{
			return _byId.TryGetValue(id ?? string.Empty, out spec);
		}
	}
}
