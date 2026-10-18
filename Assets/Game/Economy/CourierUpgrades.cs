using System;
using System.Collections.Generic;
using RD.Wallet;

namespace NightCourier.Economy
{
	public enum UpgradePurchaseStatus
	{
		Purchased = 0,
		MaxLevel = 1,
		InsufficientFunds = 2
	}

	public sealed class CourierUpgrades
	{
		private readonly UpgradeBook _book;
		private readonly IWallet _wallet;
		private readonly CourierStats _stats;
		private readonly Dictionary<string, int> _levels = new(StringComparer.Ordinal);

		public CourierUpgrades(UpgradeBook book, IWallet wallet, CourierStats stats)
		{
			_book = book ?? throw new ArgumentNullException(nameof(book));
			_wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
			_stats = stats ?? throw new ArgumentNullException(nameof(stats));

			foreach (UpgradeSpec spec in _book.All)
			{
				if (!_stats.Defines(spec.StatId))
				{
					throw new ArgumentException($"Upgrade '{spec.Id}' targets the unknown stat '{spec.StatId}'.", nameof(book));
				}
			}
		}

		public UpgradeBook Book => _book;

		public event Action Changed;

		public int GetLevel(string upgradeId)
		{
			return _levels.TryGetValue(upgradeId, out int level) ? level : 0;
		}

		public bool IsMaxed(UpgradeSpec spec)
		{
			return GetLevel(spec.Id) >= spec.MaxLevel;
		}

		public UpgradePurchaseStatus TryPurchase(string upgradeId)
		{
			UpgradeSpec spec = _book.Get(upgradeId);
			int level = GetLevel(spec.Id);

			if (level >= spec.MaxLevel)
			{
				return UpgradePurchaseStatus.MaxLevel;
			}

			WalletOperationResult result = _wallet.Spend(GameCurrencies.Credits, spec.PriceOfNextLevel(level));

			if (!result.IsSuccess)
			{
				if (result.Status == WalletOperationStatus.InsufficientFunds)
				{
					return UpgradePurchaseStatus.InsufficientFunds;
				}

				throw new InvalidOperationException($"The purchase of '{spec.Id}' failed: {result.Status}. {result.Message}");
			}

			SetLevel(spec, level + 1);
			Changed?.Invoke();

			return UpgradePurchaseStatus.Purchased;
		}

		public List<UpgradeLevelSave> Capture()
		{
			List<UpgradeLevelSave> saved = new();

			foreach (UpgradeSpec spec in _book.All)
			{
				int level = GetLevel(spec.Id);

				if (level > 0)
				{
					saved.Add(new UpgradeLevelSave { Id = spec.Id, Level = level });
				}
			}

			return saved;
		}

		public void Restore(IEnumerable<UpgradeLevelSave> saved)
		{
			foreach (UpgradeSpec spec in _book.All)
			{
				SetLevel(spec, 0);
			}

			foreach (UpgradeLevelSave entry in saved ?? Array.Empty<UpgradeLevelSave>())
			{
				if (entry != null && _book.TryGet(entry.Id, out UpgradeSpec spec))
				{
					SetLevel(spec, Math.Clamp(entry.Level, 0, spec.MaxLevel));
				}
			}

			Changed?.Invoke();
		}

		private void SetLevel(UpgradeSpec spec, int level)
		{
			if (level > 0)
			{
				_levels[spec.Id] = level;
			}
			else
			{
				_levels.Remove(spec.Id);
			}

			_stats.SetUpgradeLevel(spec, level);
		}
	}

	[Serializable]
	public sealed class UpgradeLevelSave
	{
		public string Id;
		public int Level;
	}
}
