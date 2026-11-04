using System;
using NightCourier.App;
using NightCourier.Audio;
using RD.Foundation;
using RD.Input;
using RD.PrefabWindows;
using RD.ScreenNavigation;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class PauseMenu : MonoBehaviour
	{
		private const string PauseReason = "pause-menu";

		[SerializeField] private UiSounds _uiSounds;
		[SerializeField] private GameObject _root;
		[SerializeField] private PanelScreen _mainScreen;
		[SerializeField] private PanelScreen _settingsScreen;
		[SerializeField] private PanelScreen _controlsScreen;
		[SerializeField] private Button _resumeButton;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _controlsButton;
		[SerializeField] private Button _leaveButton;
		[SerializeField] private LocalizedLabel _leaveLabel;
		[SerializeField] private Button _mainMenuButton;
		[SerializeField] private Button _settingsBackButton;
		[SerializeField] private Button _controlsBackButton;
		[SerializeField] private SettingsPanelView _settingsView;
		[SerializeField] private WindowRoot _windowRoot;
		[SerializeField] private WindowCatalog _windowCatalog;

		private GameServices _services;
		private Action _leaveAction;
		private ScreenNavigator _navigator;
		private WindowManager _windows;
		private IDisposable _openSubscription;
		private IDisposable _pauseHandle;
		private IDisposable _menuScope;

		public bool IsOpen { get; private set; }

		public void Bind(GameServices services, string leaveLabelKey = null, Action leaveAction = null)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			_services = services;
			_leaveAction = leaveAction;

			LocalizedLabel.BindAll(gameObject, services.Localization);
			_settingsView.Bind(services.Settings);
			_uiSounds.Bind(services.Audio);

			_leaveButton.gameObject.SetActive(leaveAction != null);

			if (leaveAction != null)
			{
				_leaveLabel.SetKey(leaveLabelKey);
			}

			_mainScreen.HideImmediately();
			_settingsScreen.HideImmediately();
			_controlsScreen.HideImmediately();

			_navigator = new ScreenNavigator(new ScreenRegistry(new IScreen[] { _mainScreen, _settingsScreen, _controlsScreen }));
			_windows = new WindowManager(_windowCatalog, _windowRoot);

			_resumeButton.onClick.AddListener(Close);
			_settingsButton.onClick.AddListener(ShowSettings);
			_controlsButton.onClick.AddListener(ShowControls);
			_leaveButton.onClick.AddListener(OnLeave);
			_mainMenuButton.onClick.AddListener(OnMainMenu);
			_settingsBackButton.onClick.AddListener(OnBack);
			_controlsBackButton.onClick.AddListener(OnBack);

			_openSubscription = services.Input.Actions.Pause.SubscribePerformed(_ => Open());

			_root.SetActive(false);
		}

		public void SetLeaveAvailable(bool available)
		{
			_leaveButton.gameObject.SetActive(available && _leaveAction != null);
		}

		public void Open()
		{
			if (IsOpen || _services == null)
			{
				return;
			}

			IsOpen = true;

			_pauseHandle = _services.Pause.RequestPause(PauseReason);
			_menuScope = _services.Input.PushMenu(OnCancel);

			_root.SetActive(true);
			_uiSounds.PlayOpen();

			Run(() => _navigator.PushAsync(_mainScreen.Id));
		}

		public void Close()
		{
			if (!IsOpen)
			{
				return;
			}

			IsOpen = false;

			_uiSounds.PlayBack();
			_menuScope?.Dispose();
			_pauseHandle?.Dispose();
			_menuScope = null;
			_pauseHandle = null;

			Run(CloseScreensAsync);
		}

		private void OnDestroy()
		{
			_openSubscription?.Dispose();
			_menuScope?.Dispose();
			_pauseHandle?.Dispose();
			_windows?.Dispose();
			_navigator?.Dispose();
		}

		private async Awaitable CloseScreensAsync()
		{
			await _navigator.ClearAsync();

			if (!IsOpen && _root != null)
			{
				_root.SetActive(false);
			}
		}

		private void Run(Func<Awaitable> operation)
		{
			UiShutdown.Run(operation, this);
		}

		private void OnCancel()
		{
			if (_windows.IsOpen(ConfirmWindow.WindowId))
			{
				Run(() => _windows.BackAsync());
			}
			else if (_navigator.Depth > 1)
			{
				OnBack();
			}
			else
			{
				Close();
			}
		}

		private void OnBack()
		{
			Run(async () => await _navigator.PopAsync());
		}

		private void ShowSettings()
		{
			Run(() => _navigator.PushAsync(_settingsScreen.Id));
		}

		private void ShowControls()
		{
			Run(() => _navigator.PushAsync(_controlsScreen.Id));
		}

		private void Confirm(string titleKey, string messageKey, Action onConfirm)
		{
			ConfirmArguments arguments = new(_services.Localization, _services.Audio, titleKey, messageKey, onConfirm);

			Run(() => _windows.OpenAsync(ConfirmWindow.WindowId, arguments));
		}

		private void OnLeave()
		{
			Confirm("confirm.abort.title", "confirm.abort.message", ConfirmLeave);
		}

		private void ConfirmLeave()
		{
			Close();
			_leaveAction();
		}

		private void OnMainMenu()
		{
			Confirm("confirm.main_menu.title", "confirm.main_menu.message", ConfirmMainMenu);
		}

		private async void ConfirmMainMenu()
		{
			Close();

			try
			{
				await _services.Navigator.GoToAsync(SceneIds.MainMenu);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
		}
	}
}
