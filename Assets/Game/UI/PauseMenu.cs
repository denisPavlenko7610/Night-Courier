using System;
using NightCourier.App;
using RD.Foundation;
using RD.Input;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class PauseMenu : MonoBehaviour
	{
		private const string PauseReason = "pause-menu";

		[SerializeField] private GameObject _root;
		[SerializeField] private GameObject _mainPanel;
		[SerializeField] private GameObject _settingsPanel;
		[SerializeField] private GameObject _controlsPanel;
		[SerializeField] private Button _resumeButton;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _controlsButton;
		[SerializeField] private Button _leaveButton;
		[SerializeField] private LocalizedLabel _leaveLabel;
		[SerializeField] private Button _mainMenuButton;
		[SerializeField] private Button _settingsBackButton;
		[SerializeField] private Button _controlsBackButton;
		[SerializeField] private SettingsPanelView _settingsView;

		private GameServices _services;
		private Action _leaveAction;
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

			_leaveButton.gameObject.SetActive(leaveAction != null);

			if (leaveAction != null)
			{
				_leaveLabel.SetKey(leaveLabelKey);
			}

			_resumeButton.onClick.AddListener(Close);
			_settingsButton.onClick.AddListener(ShowSettings);
			_controlsButton.onClick.AddListener(ShowControls);
			_leaveButton.onClick.AddListener(OnLeave);
			_mainMenuButton.onClick.AddListener(OnMainMenu);
			_settingsBackButton.onClick.AddListener(ShowMain);
			_controlsBackButton.onClick.AddListener(ShowMain);

			_openSubscription = services.Input.Actions.Pause.SubscribePerformed(_ => Open());

			_root.SetActive(false);
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

			ShowMain();
			_root.SetActive(true);
		}

		public void Close()
		{
			if (!IsOpen)
			{
				return;
			}

			IsOpen = false;

			_root.SetActive(false);
			_menuScope?.Dispose();
			_pauseHandle?.Dispose();
			_menuScope = null;
			_pauseHandle = null;
		}

		private void OnDestroy()
		{
			_openSubscription?.Dispose();
			_menuScope?.Dispose();
			_pauseHandle?.Dispose();
		}

		private void OnCancel()
		{
			if (_mainPanel.activeSelf)
			{
				Close();
			}
			else
			{
				ShowMain();
			}
		}

		private void ShowMain()
		{
			_mainPanel.SetActive(true);
			_settingsPanel.SetActive(false);
			_controlsPanel.SetActive(false);
		}

		private void ShowSettings()
		{
			_mainPanel.SetActive(false);
			_settingsPanel.SetActive(true);
		}

		private void ShowControls()
		{
			_mainPanel.SetActive(false);
			_controlsPanel.SetActive(true);
		}

		private void OnLeave()
		{
			Close();
			_leaveAction();
		}

		private async void OnMainMenu()
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
