using System;
using System.Collections.Generic;

namespace NightCourier.Economy
{
	public sealed class UpgradeBook
	{
		private readonly List<UpgradeSpec> _all;
		private readonly Dictionary<string, UpgradeSpec> _byId = new(StringComparer.Ordinal);

		public UpgradeBook(IEnumerable<UpgradeSpec> specs)
		{
			_all = new List<UpgradeSpec>(specs ?? throw new ArgumentNullException(nameof(specs)));

			foreach (UpgradeSpec spec in _all)
			{
				if (spec == null)
				{
					throw new ArgumentException("The upgrade book contains an empty entry.", nameof(specs));
				}

				if (!_byId.TryAdd(spec.Id, spec))
				{
					throw new ArgumentException($"Duplicate upgrade id '{spec.Id}'.", nameof(specs));
				}
			}
		}

		public static UpgradeBook Empty { get; } = new(Array.Empty<UpgradeSpec>());

		public IReadOnlyList<UpgradeSpec> All => _all;

		public bool TryGet(string id, out UpgradeSpec spec)
		{
			return _byId.TryGetValue(id ?? string.Empty, out spec);
		}

		public UpgradeSpec Get(string id)
		{
			return TryGet(id, out UpgradeSpec spec)
				? spec
				: throw new KeyNotFoundException($"Unknown upgrade '{id}'.");
		}
	}
}
