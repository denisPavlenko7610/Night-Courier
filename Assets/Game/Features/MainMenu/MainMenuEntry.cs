using System;
using System.IO;
using NightCourier.App;
using NightCourier.Audio;
using RD.DependencyInjection;
using RD.Foundation;
using RD.PrefabWindows;
using RD.Save;
using RD.ScreenNavigation;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class MainMenuEntry : MonoBehaviour, ISceneEntry
	{
		[SerializeField] private UiSounds _uiSounds;
		[SerializeField] private PanelScreen _mainScreen;
		[SerializeField] private PanelScreen _settingsScreen;
		[SerializeField] private Button _newGameButton;
		[SerializeField] private Button _continueButton;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _quitButton;
		[SerializeField] private Button _settingsBackButton;
		[SerializeField] private SettingsPanelView _settingsView;
		[SerializeField] private WindowRoot _windowRoot;
		[SerializeField] private WindowCatalog _windowCatalog;

		private GameServices _services;
		private ScreenNavigator _navigator;
		private WindowManager _windows;
		private IDisposable _menuScope;

		public void Enter(GameServices services, IObjectResolver scene)
		{
			_services = services;

			LocalizedLabel.BindAll(gameObject, services.Localization);
			_settingsView.Bind(services.Settings);
			_uiSounds.Bind(services.Audio);

			_continueButton.interactable = services.Campaign.HasSave;

			_mainScreen.HideImmediately();
			_settingsScreen.HideImmediately();

			_navigator = new ScreenNavigator(new ScreenRegistry(new IScreen[] { _mainScreen, _settingsScreen }));
			_windows = new WindowManager(_windowCatalog, _windowRoot);

			_newGameButton.onClick.AddListener(OnNewGame);
			_continueButton.onClick.AddListener(OnContinue);
			_settingsButton.onClick.AddListener(OnSettings);
			_settingsBackButton.onClick.AddListener(OnBack);
			_quitButton.onClick.AddListener(OnQuit);

			_menuScope = services.Input.PushMenu(OnCancel);

			Run(() => _navigator.PushAsync(_mainScreen.Id));
		}

		private void OnDestroy()
		{
			_menuScope?.Dispose();
			_windows?.Dispose();
			_navigator?.Dispose();

			_newGameButton.onClick.RemoveListener(OnNewGame);
			_continueButton.onClick.RemoveListener(OnContinue);
			_settingsButton.onClick.RemoveListener(OnSettings);
			_settingsBackButton.onClick.RemoveListener(OnBack);
			_quitButton.onClick.RemoveListener(OnQuit);
		}

		private void Run(Func<Awaitable> operation)
		{
			UiShutdown.Run(operation, this);
		}

		private void OnSettings()
		{
			Run(() => _navigator.PushAsync(_settingsScreen.Id));
		}

		private void OnBack()
		{
			Run(async () => await _navigator.PopAsync());
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
		}

		private void OnQuit()
		{
			_services.Navigator.Quit();
		}

		private void OnNewGame()
		{
			if (!_services.Campaign.HasSave)
			{
				StartNewGame();
				return;
			}

			ConfirmArguments arguments = new(
				_services.Localization,
				_services.Audio,
				"confirm.new_game.title",
				"confirm.new_game.message",
				StartNewGame);

			Run(() => _windows.OpenAsync(ConfirmWindow.WindowId, arguments));
		}

		private async void StartNewGame()
		{
			try
			{
				_services.Campaign.StartNew();
				await _services.Navigator.GoToAsync(SceneIds.Depot);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
		}

		private async void OnContinue()
		{
			try
			{
				await _services.Campaign.ContinueAsync();
			}
			catch (Exception exception) when (exception is InvalidSaveDataException or FileNotFoundException)
			{
				UnityLogger.LogError($"The campaign save could not be loaded: {exception.Message}", this);
				await _services.Campaign.RefreshAsync();
				_continueButton.interactable = _services.Campaign.HasSave;
				return;
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
				return;
			}

			try
			{
				await _services.Navigator.GoToAsync(SceneIds.Depot);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
		}
	}
}
