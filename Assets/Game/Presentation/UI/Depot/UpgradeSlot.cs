using System;
using NightCourier.Economy;
using RD.Localization;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class UpgradeSlot : IDisposable
	{
		public const string BuyButtonName = "upgrade-buy";

		private const string LevelKey = "upgrades.level";
		private const string BuyKey = "upgrades.buy";
		private const string MaxedKey = "upgrades.maxed";
		private const string TitleName = "row-title";
		private const string DescriptionName = "row-description";
		private const string LevelName = "row-level";

		private readonly VisualElement _row;
		private readonly ILocalizationService _localization;
		private readonly Label _title;
		private readonly Label _description;
		private readonly Label _level;
		private readonly Button _button;

		private UpgradeSpec _spec;
		private Action<UpgradeSpec> _onBuy;

		public UpgradeSlot(VisualElement row, ILocalizationService localization)
		{
			_row = row ?? throw new ArgumentNullException(nameof(row));
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));
			_title = UiQuery.Require<Label>(row, TitleName);
			_description = UiQuery.Require<Label>(row, DescriptionName);
			_level = UiQuery.Require<Label>(row, LevelName);
			_button = UiQuery.Require<Button>(row, BuyButtonName);

			_button.clicked += OnClicked;

			Hide();
		}

		public Button Button => _button;

		public void Show(UpgradeSpec spec, int level, long credits, Action<UpgradeSpec> onBuy)
		{
			_spec = spec;
			_onBuy = onBuy;

			UiQuery.SetVisible(_row, true);

			_title.text = _localization.Get(spec.TitleKey);
			_description.text = _localization.Get(spec.DescriptionKey);
			_level.text = _localization.Get(
				LevelKey,
				level,
				spec.MaxLevel,
				UiStyle.Tint(UiStyle.Pips(level, spec.MaxLevel), UiStyle.AccentColor));

			if (level >= spec.MaxLevel)
			{
				_button.text = _localization.Get(MaxedKey);
				_button.SetEnabled(false);
				return;
			}

			long price = spec.PriceOfNextLevel(level);

			_button.text = _localization.Get(BuyKey, price);
			_button.SetEnabled(credits >= price);
		}

		public void Hide()
		{
			_spec = null;

			UiQuery.SetVisible(_row, false);
		}

		public void Dispose()
		{
			_button.clicked -= OnClicked;
		}

		private void OnClicked()
		{
			if (_spec != null)
			{
				_onBuy?.Invoke(_spec);
			}
		}
	}
}
