using System;
using NightCourier.App;
using NightCourier.Audio;
using RD.Foundation;
using RD.Input;
using RD.Menus;
using RD.ScreenNavigation;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class PauseMenu : MonoBehaviour
	{
		public const string ResumeEntryId = "resume";
		public const string SettingsEntryId = "settings";
		public const string ControlsEntryId = "controls";
		public const string LeaveEntryId = "leave";
		public const string MainMenuEntryId = "main-menu";
		public const string MainScreenId = "pause.main";
		public const string SettingsScreenId = "pause.settings";
		public const string ControlsScreenId = "pause.controls";

		private const string PauseReason = "pause-menu";
		private const string MenuId = "pause";
		private const string TitleKey = "pause.title";
		private const string ResumeKey = "pause.resume";
		private const string SettingsKey = "pause.settings";
		private const string ControlsKey = "pause.controls";
		private const string DefaultLeaveKey = "pause.abort_contract";
		private const string MainMenuKey = "pause.main_menu";
		private const string AbortConfirmTitleKey = "confirm.abort.title";
		private const string AbortConfirmMessageKey = "confirm.abort.message";
		private const string MainMenuConfirmTitleKey = "confirm.main_menu.title";
		private const string MainMenuConfirmMessageKey = "confirm.main_menu.message";
		private const string LayerName = "layer";
		private const string MenuName = "menu";
		private const string SettingsSlotName = "settings-instance";
		private const string SettingsName = "settings";
		private const string ControlsSlotName = "controls-instance";
		private const string ControlsName = "controls";
		private const string ConfirmSlotName = "confirm-instance";

		[SerializeField] private UiSounds _uiSounds;
		[SerializeField] private UIDocument _document;
		[SerializeField] private MenuAssets _menuAssets;

		private GameServices _services;
		private Action _leaveAction;
		private bool _leaveAvailable = true;
		private VisualElement _layer;
		private Menu _menu;
		private MenuView _mainView;
		private SettingsPageView _settingsView;
		private ConfirmDialog _dialog;
		private ScreenNavigator _navigator;
		private IDisposable _openSubscription;
		private IDisposable _pauseHandle;
		private IDisposable _menuScope;
		private Button _controlsBack;
		private int _backFrame = -1;

		public bool IsOpen { get; private set; }

		public bool IsLeaveAvailable => _leaveAction != null && _leaveAvailable;

		public Menu Menu => _menu;

		public SettingsPageView SettingsView => _settingsView;

		public ConfirmDialog Dialog => _dialog;

		public IScreenNavigator Navigator => _navigator;

		public VisualElement Root => _document.rootVisualElement;

		public void Bind(GameServices services, string leaveLabelKey = null, Action leaveAction = null)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			_services = services;
			_leaveAction = leaveAction;

			VisualElement root = _document.rootVisualElement;
			IMenuTextProvider text = GameMenuText.Create(services.Localization);

			_layer = root.Q<VisualElement>(LayerName);

			LocalizedText.Apply(root, services.Localization);
			_uiSounds.Bind(services.Audio, root);

			_menu = new Menu(MenuId, TitleKey);
			_menu.Add(ResumeEntryId, ResumeKey, Close);
			_menu.Add(SettingsEntryId, SettingsKey, () => PushAsync(SettingsScreenId));
			_menu.Add(ControlsEntryId, ControlsKey, () => PushAsync(ControlsScreenId));
			_menu.Add(
				LeaveEntryId,
				string.IsNullOrEmpty(leaveLabelKey) ? DefaultLeaveKey : leaveLabelKey,
				OnLeave,
				isVisible: () => _leaveAction != null && _leaveAvailable);
			_menu.Add(MainMenuEntryId, MainMenuKey, OnMainMenu);

			_mainView = new MenuView(_menu, root.Q<VisualElement>(MenuName), _menuAssets, text, MenuProfile.Desktop);
			_settingsView = new SettingsPageView(
				services.SettingsPage,
				root.Q<VisualElement>(SettingsSlotName).Q<VisualElement>(SettingsName),
				_menuAssets,
				text,
				MenuProfile.Desktop,
				SettingsApplyMode.Explicit);

			VisualElement controls = root.Q<VisualElement>(ControlsSlotName).Q<VisualElement>(ControlsName);

			_controlsBack = controls.Q<Button>("controls-back");
			_dialog = new ConfirmDialog(root.Q<VisualElement>(ConfirmSlotName).Q<VisualElement>(ConfirmDialog.RootName), services.Localization);

			_navigator = new ScreenNavigator(new ScreenRegistry(new IScreen[]
			{
				UiScreen.ForView(MainScreenId, _mainView, destroyCancellationToken),
				UiScreen.ForView(SettingsScreenId, _settingsView, destroyCancellationToken),
				UiScreen.ForElement(ControlsScreenId, controls, destroyCancellationToken)
			}));

			_settingsView.BackRequested += OnBack;
			_controlsBack.clicked += OnBack;
			_openSubscription = services.Input.Actions.Pause.SubscribePerformed(_ => Open());

			_layer.style.display = DisplayStyle.None;
		}

		public void SetLeaveAvailable(bool available)
		{
			_leaveAvailable = available;
			_menu?.Refresh();
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

			_layer.style.display = DisplayStyle.Flex;
			_uiSounds.PlayOpen();

			Run(() => _navigator.PushAsync(MainScreenId));
		}

		public void Close()
		{
			if (!IsOpen)
			{
				return;
			}

			IsOpen = false;

			_uiSounds.PlayBack();
			_dialog.Close();
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

			if (_settingsView != null)
			{
				_settingsView.BackRequested -= OnBack;
			}

			if (_controlsBack != null)
			{
				_controlsBack.clicked -= OnBack;
			}

			_dialog?.Dispose();
			_navigator?.Dispose();
			_settingsView?.Dispose();
			_mainView?.Dispose();
		}

		private async Awaitable CloseScreensAsync()
		{
			await _navigator.ClearAsync();

			if (!IsOpen && _layer != null)
			{
				_layer.style.display = DisplayStyle.None;
			}
		}

		private void Run(Func<Awaitable> operation)
		{
			UiShutdown.Run(operation, this);
		}

		private async Awaitable PushAsync(string screenId)
		{
			try
			{
				await _navigator.PushAsync(screenId);
			}
			catch (Exception exception) when (UiShutdown.IsExpected(exception))
			{
			}
		}

		private void OnCancel()
		{
			if (_dialog.IsOpen)
			{
				_dialog.Close();
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
			if (Time.frameCount == _backFrame || _navigator.Depth <= 1)
			{
				return;
			}

			_backFrame = Time.frameCount;

			if (_navigator.CurrentScreenId == SettingsScreenId)
			{
				_services.SettingsPage.Revert();
			}

			Run(async () => await _navigator.PopAsync());
		}

		private void OnLeave()
		{
			_dialog.Open(AbortConfirmTitleKey, AbortConfirmMessageKey, ConfirmLeave);
		}

		private void ConfirmLeave()
		{
			Close();
			_leaveAction();
		}

		private void OnMainMenu()
		{
			_dialog.Open(MainMenuConfirmTitleKey, MainMenuConfirmMessageKey, ConfirmMainMenu);
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
