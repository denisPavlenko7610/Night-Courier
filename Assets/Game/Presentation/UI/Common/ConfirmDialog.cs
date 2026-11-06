using System;
using RD.Localization;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class ConfirmDialog : IDisposable
	{
		public const string RootName = "confirm";
		public const string ConfirmButtonName = "confirm-ok";
		public const string CancelButtonName = "confirm-cancel";

		private const string TitleName = "confirm-title";
		private const string MessageName = "confirm-message";

		private readonly VisualElement _root;
		private readonly ILocalizationService _localization;
		private readonly Label _title;
		private readonly Label _message;
		private readonly Button _confirm;
		private readonly Button _cancel;

		private Action _onConfirm;
		private VisualElement _previousFocus;
		private bool _isDisposed;

		public ConfirmDialog(VisualElement root, ILocalizationService localization)
		{
			_root = root ?? throw new ArgumentNullException(nameof(root));
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));
			_title = Require<Label>(TitleName);
			_message = Require<Label>(MessageName);
			_confirm = Require<Button>(ConfirmButtonName);
			_cancel = Require<Button>(CancelButtonName);

			LocalizedText.Apply(root, localization);

			_root.style.display = DisplayStyle.None;
			_confirm.clicked += OnConfirmClicked;
			_cancel.clicked += Close;
			_root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
		}

		public bool IsOpen { get; private set; }

		public void Open(string titleKey, string messageKey, Action onConfirm)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(ConfirmDialog));
			}

			_onConfirm = onConfirm ?? throw new ArgumentNullException(nameof(onConfirm));

			if (!IsOpen)
			{
				_previousFocus = _root.panel?.focusController.focusedElement as VisualElement;
			}

			LocalizedText.SetKey(_title, _localization, titleKey);
			LocalizedText.SetKey(_message, _localization, messageKey);

			IsOpen = true;
			_root.style.display = DisplayStyle.Flex;
			SetSiblingsEnabled(false);
			_root.schedule.Execute(() =>
				{
					if (IsOpen)
					{
						_cancel.Focus();
					}
				}
			);
		}

		public void Close()
		{
			if (!IsOpen)
			{
				return;
			}

			IsOpen = false;
			_onConfirm = null;
			_root.style.display = DisplayStyle.None;
			SetSiblingsEnabled(true);

			if (_previousFocus != null && _previousFocus.panel != null && _previousFocus.enabledInHierarchy)
			{
				_previousFocus.Focus();
			}

			_previousFocus = null;
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			Close();

			_isDisposed = true;
			_confirm.clicked -= OnConfirmClicked;
			_cancel.clicked -= Close;
			_root.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
		}

		private void OnConfirmClicked()
		{
			Action action = _onConfirm;

			Close();
			action?.Invoke();
		}

		private void OnNavigationCancel(NavigationCancelEvent evt)
		{
			evt.StopPropagation();
			Close();
		}

		private void SetSiblingsEnabled(bool enabled)
		{
			VisualElement slot = _root.parent;
			VisualElement layer = slot?.parent;

			if (layer == null)
			{
				return;
			}

			foreach (VisualElement sibling in layer.Children())
			{
				if (sibling != slot)
				{
					sibling.SetEnabled(enabled);
				}
			}
		}

		private T Require<T>(string name) where T : VisualElement
		{
			return _root.Q<T>(name) ??
				throw new InvalidOperationException(
					$"The confirm dialog UXML requires a {typeof(T).Name} named '{name}'."
				);
		}
	}
}