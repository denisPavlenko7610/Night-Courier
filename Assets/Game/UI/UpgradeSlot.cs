using System;
using NightCourier.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class UpgradeSlot : MonoBehaviour
	{
		private const string LevelKey = "upgrades.level";
		private const string BuyKey = "upgrades.buy";
		private const string MaxedKey = "upgrades.maxed";

		[SerializeField] private LocalizedLabel _title;
		[SerializeField] private LocalizedLabel _description;
		[SerializeField] private LocalizedLabel _level;
		[SerializeField] private Button _button;
		[SerializeField] private LocalizedLabel _buttonLabel;

		private UpgradeSpec _spec;
		private Action<UpgradeSpec> _onBuy;

		public void Show(UpgradeSpec spec, int level, long credits, Action<UpgradeSpec> onBuy)
		{
			_spec = spec;
			_onBuy = onBuy;

			gameObject.SetActive(true);
			_title.SetKey(spec.TitleKey);
			_description.SetKey(spec.DescriptionKey);
			_level.SetKey(LevelKey, level, spec.MaxLevel);

			if (level >= spec.MaxLevel)
			{
				_buttonLabel.SetKey(MaxedKey);
				_button.interactable = false;
				return;
			}

			long price = spec.PriceOfNextLevel(level);

			_buttonLabel.SetKey(BuyKey, price);
			_button.interactable = credits >= price;
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		private void Awake()
		{
			_button.onClick.AddListener(OnClicked);
		}

		private void OnDestroy()
		{
			_button.onClick.RemoveListener(OnClicked);
		}

		private void OnClicked()
		{
			_onBuy?.Invoke(_spec);
		}
	}
}
