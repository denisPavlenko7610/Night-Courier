using System;
using RD.Localization;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class LockerRow : IDisposable
	{
		public const string ActionButtonName = "locker-action";

		private const string TitleName = "row-title";
		private const string StatusName = "row-status";

		private readonly VisualElement _row;
		private readonly ILocalizationService _localization;
		private readonly Label _title;
		private readonly Label _status;
		private readonly Button _button;

		private Action _onClick;

		public LockerRow(VisualElement row, ILocalizationService localization)
		{
			_row = row ?? throw new ArgumentNullException(nameof(row));
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));
			_title = UiQuery.Require<Label>(row, TitleName);
			_status = UiQuery.Require<Label>(row, StatusName);
			_button = UiQuery.Require<Button>(row, ActionButtonName);

			_button.clicked += OnClicked;

			Hide();
		}

		public Button Button => _button;

		public void Show(
			string titleKey,
			string statusKey,
			object[] statusArguments,
			string buttonKey,
			object[] buttonArguments,
			bool interactable,
			Action onClick)
		{
			_onClick = onClick;

			UiQuery.SetVisible(_row, true);

			_title.text = _localization.Get(titleKey);
			_status.text = _localization.Get(statusKey, statusArguments);
			_button.text = _localization.Get(buttonKey, buttonArguments);
			_button.SetEnabled(interactable);
		}

		public void Hide()
		{
			_onClick = null;

			UiQuery.SetVisible(_row, false);
		}

		public void Dispose()
		{
			_button.clicked -= OnClicked;
		}

		private void OnClicked()
		{
			_onClick?.Invoke();
		}
	}
}
