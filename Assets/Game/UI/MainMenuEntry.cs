using System;
using System.IO;
using NightCourier.App;
using RD.DependencyInjection;
using RD.Foundation;
using RD.Save;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class MainMenuEntry : MonoBehaviour, ISceneEntry
	{
		[SerializeField] private GameObject _mainPanel;
		[SerializeField] private GameObject _settingsPanel;
		[SerializeField] private Button _newGameButton;
		[SerializeField] private Button _continueButton;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _quitButton;
		[SerializeField] private Button _settingsBackButton;
		[SerializeField] private SettingsPanelView _settingsView;

		private GameServices _services;

		public void Enter(GameServices services, IObjectResolver scene)
		{
			_services = services;

			LocalizedLabel.BindAll(gameObject, services.Localization);
			_settingsView.Bind(services.Settings);

			_continueButton.interactable = services.Campaign.HasSave;

			_newGameButton.onClick.AddListener(OnNewGame);
			_continueButton.onClick.AddListener(OnContinue);
			_settingsButton.onClick.AddListener(ShowSettings);
			_settingsBackButton.onClick.AddListener(ShowMain);
			_quitButton.onClick.AddListener(OnQuit);

			ShowMain();
		}

		private void OnDestroy()
		{
			_newGameButton.onClick.RemoveListener(OnNewGame);
			_continueButton.onClick.RemoveListener(OnContinue);
			_settingsButton.onClick.RemoveListener(ShowSettings);
			_settingsBackButton.onClick.RemoveListener(ShowMain);
			_quitButton.onClick.RemoveListener(OnQuit);
		}

		private void ShowMain()
		{
			_mainPanel.SetActive(true);
			_settingsPanel.SetActive(false);
		}

		private void ShowSettings()
		{
			_mainPanel.SetActive(false);
			_settingsPanel.SetActive(true);
		}

		private void OnQuit()
		{
			_services.Navigator.Quit();
		}

		private async void OnNewGame()
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
