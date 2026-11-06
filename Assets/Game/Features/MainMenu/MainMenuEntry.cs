using System;
using System.IO;
using NightCourier.App;
using NightCourier.Audio;
using RD.DependencyInjection;
using RD.Foundation;
using RD.Menus;
using RD.Save;
using RD.ScreenNavigation;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class MainMenuEntry : MonoBehaviour, ISceneEntry
	{
		public const string NewGameEntryId = "new-game";
		public const string ContinueEntryId = "continue";
		public const string SettingsEntryId = "settings";
		public const string QuitEntryId = "quit";
		public const string MainScreenId = "main";
		public const string SettingsScreenId = "settings";

		private const string MenuId = "main-menu";
		private const string MenuTitleKey = "menu.title";
		private const string NewGameKey = "menu.new_game";
		private const string ContinueKey = "menu.continue";
		private const string SettingsKey = "menu.settings";
		private const string QuitKey = "menu.quit";
		private const string NewGameConfirmTitleKey = "confirm.new_game.title";
		private const string NewGameConfirmMessageKey = "confirm.new_game.message";
		private const string MenuName = "menu";
		private const string SettingsSlotName = "settings-instance";
		private const string SettingsName = "settings";
		private const string ConfirmSlotName = "confirm-instance";

		[SerializeField] private UiSounds _uiSounds;
		[SerializeField] private UIDocument _document;
		[SerializeField] private MenuAssets _menuAssets;

		private GameServices _services;
		private Menu _menu;
		private MenuView _mainView;
		private SettingsPageView _settingsView;
		private ConfirmDialog _dialog;
		private ScreenNavigator _navigator;
		private IDisposable _menuScope;
		private int _backFrame = -1;

		public Menu Menu => _menu;

		public SettingsPageView SettingsView => _settingsView;

		public ConfirmDialog Dialog => _dialog;

		public IScreenNavigator Navigator => _navigator;

		public VisualElement Root => _document.rootVisualElement;

		public void Enter(GameServices services, IObjectResolver scene)
		{
			_services = services;

			VisualElement root = _document.rootVisualElement;
			IMenuTextProvider text = GameMenuText.Create(services.Localization);

			LocalizedText.Apply(root, services.Localization);
			_uiSounds.Bind(services.Audio, root);

			_menu = new Menu(MenuId, MenuTitleKey);
			_menu.Add(NewGameEntryId, NewGameKey, OnNewGame);
			_menu.Add(ContinueEntryId, ContinueKey, ContinueAsync, isEnabled: () => _services.Campaign.HasSave);
			_menu.Add(SettingsEntryId, SettingsKey, OpenSettingsAsync);
			_menu.Add(QuitEntryId, QuitKey, OnQuit, isVisible: () => MenuProfiles.CanQuit);

			_mainView = new MenuView(_menu, root.Q<VisualElement>(MenuName), _menuAssets, text, MenuProfile.Desktop);
			_settingsView = new SettingsPageView(
				services.SettingsPage,
				root.Q<VisualElement>(SettingsSlotName).Q<VisualElement>(SettingsName),
				_menuAssets,
				text,
				MenuProfile.Desktop,
				SettingsApplyMode.Explicit);
			_dialog = new ConfirmDialog(root.Q<VisualElement>(ConfirmSlotName).Q<VisualElement>(ConfirmDialog.RootName), services.Localization);

			_navigator = new ScreenNavigator(new ScreenRegistry(new IScreen[]
			{
				UiScreen.ForView(MainScreenId, _mainView, destroyCancellationToken),
				UiScreen.ForView(SettingsScreenId, _settingsView, destroyCancellationToken)
			}));

			_settingsView.BackRequested += OnBack;
			_menuScope = services.Input.PushMenu(OnCancel);

			Run(() => _navigator.PushAsync(MainScreenId));
		}

		private void OnDestroy()
		{
			_menuScope?.Dispose();

			if (_settingsView != null)
			{
				_settingsView.BackRequested -= OnBack;
			}

			_dialog?.Dispose();
			_navigator?.Dispose();
			_settingsView?.Dispose();
			_mainView?.Dispose();
		}

		private void Run(Func<Awaitable> operation)
		{
			UiShutdown.Run(operation, this);
		}

		private async Awaitable OpenSettingsAsync()
		{
			try
			{
				await _navigator.PushAsync(SettingsScreenId);
			}
			catch (Exception exception) when (UiShutdown.IsExpected(exception))
			{
			}
		}

		private void OnBack()
		{
			if (Time.frameCount == _backFrame || _navigator.Depth <= 1)
			{
				return;
			}

			_backFrame = Time.frameCount;
			_services.SettingsPage.Revert();

			Run(async () => await _navigator.PopAsync());
		}

		private void OnCancel()
		{
			if (_dialog.IsOpen)
			{
				_dialog.Close();
			}
			else
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

			_dialog.Open(NewGameConfirmTitleKey, NewGameConfirmMessageKey, StartNewGame);
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

		private async Awaitable ContinueAsync()
		{
			try
			{
				await _services.Campaign.ContinueAsync();
			}
			catch (Exception exception) when (exception is InvalidSaveDataException or FileNotFoundException)
			{
				UnityLogger.LogError($"The campaign save could not be loaded: {exception.Message}", this);
				await _services.Campaign.RefreshAsync();
				_menu.Refresh();
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
