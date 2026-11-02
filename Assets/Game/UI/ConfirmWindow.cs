using System;
using RD.Localization;
using RD.PrefabWindows;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class ConfirmArguments
	{
		public const string ConfirmKey = "common.confirm";
		public const string CancelKey = "common.cancel";

		public ConfirmArguments(ILocalizationService localization, string titleKey, string messageKey, Action onConfirm)
		{
			Localization = localization ?? throw new ArgumentNullException(nameof(localization));
			TitleKey = titleKey ?? throw new ArgumentNullException(nameof(titleKey));
			MessageKey = messageKey ?? throw new ArgumentNullException(nameof(messageKey));
			OnConfirm = onConfirm ?? throw new ArgumentNullException(nameof(onConfirm));
		}

		public ILocalizationService Localization { get; }

		public string TitleKey { get; }

		public string MessageKey { get; }

		public Action OnConfirm { get; }
	}

	public sealed class ConfirmWindow : UIWindow<ConfirmArguments>
	{
		public const string WindowId = "confirm";

		[SerializeField] private LocalizedLabel _title;
		[SerializeField] private LocalizedLabel _message;
		[SerializeField] private LocalizedLabel _confirmLabel;
		[SerializeField] private LocalizedLabel _cancelLabel;
		[SerializeField] private Button _confirmButton;
		[SerializeField] private Button _cancelButton;

		private Action _onConfirm;

		protected override void OnInitialized()
		{
			_confirmButton.onClick.AddListener(OnConfirmClicked);
			_cancelButton.onClick.AddListener(Close);
		}

		protected override void OnOpen(ConfirmArguments arguments)
		{
			_title.Bind(arguments.Localization);
			_message.Bind(arguments.Localization);
			_confirmLabel.Bind(arguments.Localization);
			_cancelLabel.Bind(arguments.Localization);

			_title.SetKey(arguments.TitleKey);
			_message.SetKey(arguments.MessageKey);
			_confirmLabel.SetKey(ConfirmArguments.ConfirmKey);
			_cancelLabel.SetKey(ConfirmArguments.CancelKey);

			_onConfirm = arguments.OnConfirm;
		}

		protected override void OnHidden()
		{
			_onConfirm = null;
		}

		private void OnDestroy()
		{
			_confirmButton.onClick.RemoveListener(OnConfirmClicked);
			_cancelButton.onClick.RemoveListener(Close);
		}

		private void OnConfirmClicked()
		{
			Action action = _onConfirm;

			_onConfirm = null;

			Close();
			action?.Invoke();
		}
	}
}
