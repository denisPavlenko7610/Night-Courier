using System;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class WarehouseHudView : MonoBehaviour
	{
		private const string TimerKey = "warehouse.time_left";
		private const string GroupName = "hud-group";
		private const string ContractName = "contract-title";
		private const string TimerName = "timer-label";

		[SerializeField] private UIDocument _document;

		private ILocalizationService _localization;
		private VisualElement _group;
		private Label _contract;
		private Label _timer;

		public bool IsHidden { get; private set; }

		public string ContractText => _contract?.text;

		public string TimerText => _timer?.text;

		public bool TimerVisible => _timer != null && _timer.style.display != DisplayStyle.None;

		public void Bind(ILocalizationService localization, string contractTitleKey, bool hasDeadline)
		{
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));

			VisualElement root = _document.rootVisualElement;

			_group = UiQuery.Require<VisualElement>(root, GroupName);
			_contract = UiQuery.Require<Label>(root, ContractName);
			_timer = UiQuery.Require<Label>(root, TimerName);

			LocalizedText.Apply(root, localization);

			_contract.text = localization.Get(contractTitleKey);

			UiQuery.SetVisible(_timer, hasDeadline);
			UiQuery.SetVisible(_group, !IsHidden);
		}

		public void SetTimer(string clock)
		{
			_timer.text = _localization.Get(TimerKey, clock);
		}

		public void SetHidden(bool hidden)
		{
			IsHidden = hidden;

			if (_group != null)
			{
				UiQuery.SetVisible(_group, !hidden);
			}
		}
	}
}
