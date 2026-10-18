using System;
using System.Collections.Generic;
using RD.Stats;

namespace NightCourier.Economy
{
	public sealed class UpgradeSpec
	{
		private readonly long[] _prices;

		public UpgradeSpec(
			string id,
			string titleKey,
			string descriptionKey,
			string statId,
			StatModifierKind kind,
			double amountPerLevel,
			IEnumerable<long> prices)
		{
			Id = Require(id, nameof(id));
			TitleKey = Require(titleKey, nameof(titleKey));
			DescriptionKey = Require(descriptionKey, nameof(descriptionKey));
			StatId = Require(statId, nameof(statId));

			if (kind == StatModifierKind.MultiplicativePercent)
			{
				throw new ArgumentOutOfRangeException(nameof(kind), kind, "Upgrades use flat or additive percent modifiers.");
			}

			if (double.IsNaN(amountPerLevel) || double.IsInfinity(amountPerLevel) || amountPerLevel <= 0d)
			{
				throw new ArgumentOutOfRangeException(nameof(amountPerLevel), amountPerLevel, "An upgrade needs a positive amount per level.");
			}

			_prices = prices == null ? Array.Empty<long>() : new List<long>(prices).ToArray();

			if (_prices.Length == 0)
			{
				throw new ArgumentException("An upgrade needs at least one level.", nameof(prices));
			}

			foreach (long price in _prices)
			{
				if (price < 1)
				{
					throw new ArgumentOutOfRangeException(nameof(prices), price, "Upgrade prices must be positive.");
				}
			}

			Kind = kind;
			AmountPerLevel = amountPerLevel;
		}

		public string Id { get; }

		public string TitleKey { get; }

		public string DescriptionKey { get; }

		public string StatId { get; }

		public StatModifierKind Kind { get; }

		public double AmountPerLevel { get; }

		public int MaxLevel => _prices.Length;

		public long PriceOfNextLevel(int currentLevel)
		{
			if (currentLevel < 0 || currentLevel >= _prices.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(currentLevel), currentLevel, "There is no next level.");
			}

			return _prices[currentLevel];
		}

		private static string Require(string value, string name)
		{
			return string.IsNullOrWhiteSpace(value)
				? throw new ArgumentException("A value is required.", name)
				: value;
		}
	}
}
