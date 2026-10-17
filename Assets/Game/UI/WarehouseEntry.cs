using System;
using System.Text;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Player;
using RD.Foundation;
using RD.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class WarehouseEntry : MonoBehaviour, ISceneEntry
	{
		[SerializeField] private PlayerSession _player;
		[SerializeField] private InteractionPromptView _promptView;
		[SerializeField] private PauseMenu _pauseMenu;
		[SerializeField] private Interactable _deliveryPoint;
		[SerializeField] private GameObject _resultPanel;
		[SerializeField] private LocalizedLabel _contractTitle;
		[SerializeField] private LocalizedLabel _timerLabel;
		[SerializeField] private Text _breakdownText;
		[SerializeField] private LocalizedLabel _creditsLabel;
		[SerializeField] private Button _returnButton;

		private GameServices _services;
		private CampaignState _campaign;
		private ContractRun _run;
		private IDisposable _resultScope;

		public void Enter(GameServices services)
		{
			_campaign = services.Campaign.Current
				?? throw new InvalidOperationException("The warehouse requires an active campaign.");
			_run = _campaign.Contracts.ActiveRun;

			if (_run == null || _run.Phase != ContractPhase.InTransit)
			{
				throw new InvalidOperationException("The warehouse requires a contract in transit.");
			}

			_services = services;

			LocalizedLabel.BindAll(gameObject, services.Localization);
			_contractTitle.SetKey(_run.Spec.TitleKey);
			_timerLabel.gameObject.SetActive(_run.HasDeadline);

			_player.Begin(services);
			_promptView.Bind(_player.Interactor, services.Localization, services.Input.Actions);
			_pauseMenu.Bind(services, "pause.abort_contract", OnAbort);

			_deliveryPoint.Interacted += OnDelivered;
			_returnButton.onClick.AddListener(OnReturn);

			_resultPanel.SetActive(false);
		}

		private void Update()
		{
			if (_run == null || _run.IsFinished)
			{
				return;
			}

			_run.Tick(Time.deltaTime);

			if (_run.HasDeadline && _run.Phase == ContractPhase.InTransit)
			{
				_timerLabel.SetKey("warehouse.time_left", FormatClock(_run.RemainingSeconds));
			}
		}

		private void OnDestroy()
		{
			_resultScope?.Dispose();

			if (_deliveryPoint != null)
			{
				_deliveryPoint.Interacted -= OnDelivered;
			}

			_returnButton.onClick.RemoveListener(OnReturn);
		}

		private async void OnDelivered()
		{
			_deliveryPoint.SetAvailable(false);

			try
			{
				_run.Deliver();

				ContractSettlement settlement = await _campaign.Contracts.SettleAsync();
				await _services.Campaign.SaveAsync();

				ShowResult(settlement);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
				_deliveryPoint.SetAvailable(true);
			}
		}

		private void OnAbort()
		{
			_campaign.Contracts.Fail();
			Navigate(SceneIds.Depot);
		}

		private void OnReturn()
		{
			Navigate(SceneIds.Depot);
		}

		private void ShowResult(ContractSettlement settlement)
		{
			ILocalizationService localization = _services.Localization;
			StringBuilder text = new();

			foreach (SettlementLine line in settlement.Lines)
			{
				text.AppendLine(localization.Get("result.line", localization.Get(line.LabelKey), line.Amount));
			}

			text.Append(localization.Get("result.total", settlement.Total));

			_breakdownText.text = text.ToString();
			_creditsLabel.SetKey("depot.credits", _campaign.Credits);

			_resultScope = _services.Input.PushMenu();
			_resultPanel.SetActive(true);
		}

		private async void Navigate(string sceneId)
		{
			_returnButton.interactable = false;

			try
			{
				await _services.Navigator.GoToAsync(sceneId);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
				_returnButton.interactable = true;
			}
		}

		private static string FormatClock(double seconds)
		{
			int whole = Mathf.CeilToInt((float)seconds);
			return $"{whole / 60:00}:{whole % 60:00}";
		}
	}
}
