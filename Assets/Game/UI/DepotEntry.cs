using System;
using System.Collections.Generic;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Player;
using RD.Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class DepotEntry : MonoBehaviour, ISceneEntry
	{
		private const string AcceptKey = "contract.accept";
		private const string CollectKey = "contract.collect";
		private const string DepartKey = "contract.depart";

		[SerializeField] private PlayerSession _player;
		[SerializeField] private InteractionPromptView _promptView;
		[SerializeField] private PauseMenu _pauseMenu;
		[SerializeField] private Interactable _terminal;
		[SerializeField] private GameObject _terminalPanel;
		[SerializeField] private Button _closeButton;
		[SerializeField] private LocalizedLabel _creditsLabel;
		[SerializeField] private ContractSlot[] _slots;
		[SerializeField] private GameObject _emptyNotice;
		[SerializeField] private GameObject _detailsRoot;
		[SerializeField] private LocalizedLabel _detailsTitle;
		[SerializeField] private LocalizedLabel _detailsBody;
		[SerializeField] private LocalizedLabel _detailsInfo;
		[SerializeField] private Button _actionButton;
		[SerializeField] private LocalizedLabel _actionLabel;

		private GameServices _services;
		private CampaignState _campaign;
		private ContractSpec _selected;
		private IDisposable _terminalScope;

		public void Enter(GameServices services)
		{
			_campaign = services.Campaign.Current
				?? throw new InvalidOperationException("The depot requires an active campaign.");
			_services = services;

			LocalizedLabel.BindAll(gameObject, services.Localization);

			_player.Begin(services);
			_promptView.Bind(_player.Interactor, services.Localization, services.Input.Actions);
			_pauseMenu.Bind(services);

			_terminal.Interacted += OpenTerminal;
			_actionButton.onClick.AddListener(OnAction);
			_closeButton.onClick.AddListener(CloseTerminal);

			_terminalPanel.SetActive(false);
			_creditsLabel.SetKey("depot.credits", _campaign.Credits);
		}

		private void OnDestroy()
		{
			_terminalScope?.Dispose();

			if (_terminal != null)
			{
				_terminal.Interacted -= OpenTerminal;
			}

			_actionButton.onClick.RemoveListener(OnAction);
			_closeButton.onClick.RemoveListener(CloseTerminal);
		}

		private void OpenTerminal()
		{
			if (_terminalScope != null)
			{
				return;
			}

			_terminalScope = _services.Input.PushMenu(CloseTerminal);
			_terminalPanel.SetActive(true);

			Refresh();
		}

		private void CloseTerminal()
		{
			if (_terminalScope == null)
			{
				return;
			}

			_terminalScope.Dispose();
			_terminalScope = null;
			_terminalPanel.SetActive(false);
		}

		private void Refresh()
		{
			_creditsLabel.SetKey("depot.credits", _campaign.Credits);

			ContractRun run = _campaign.Contracts.ActiveRun;

			if (run != null)
			{
				ShowRun(run);
				return;
			}

			ShowOffers();
		}

		private void ShowOffers()
		{
			IReadOnlyList<ContractSpec> available = _campaign.Contracts.GetAvailable();

			if (_selected == null || !Contains(available, _selected))
			{
				_selected = available.Count > 0 ? available[0] : null;
			}

			for (int index = 0; index < _slots.Length; index++)
			{
				if (index < available.Count)
				{
					_slots[index].Show(available[index], available[index] == _selected, OnSelected);
				}
				else
				{
					_slots[index].Hide();
				}
			}

			_emptyNotice.SetActive(available.Count == 0);
			ShowDetails(_selected);
			ShowAction(_selected != null ? AcceptKey : null);
		}

		private void ShowRun(ContractRun run)
		{
			foreach (ContractSlot slot in _slots)
			{
				slot.Hide();
			}

			_emptyNotice.SetActive(false);
			ShowDetails(run.Spec);

			ShowAction(run.Phase switch
			{
				ContractPhase.Accepted => CollectKey,
				ContractPhase.PackageCollected => DepartKey,
				_ => null
			});
		}

		private void ShowDetails(ContractSpec spec)
		{
			_detailsRoot.SetActive(spec != null);

			if (spec == null)
			{
				return;
			}

			_detailsTitle.SetKey(spec.TitleKey);
			_detailsBody.SetKey(spec.DescriptionKey);
			_detailsInfo.SetKey(
				"contract.info",
				_services.Localization.Get(RiskKey(spec.Risk)),
				spec.BaseReward,
				_services.Localization.Get(StateKey(spec.LocationState)));
		}

		private void ShowAction(string key)
		{
			_actionButton.gameObject.SetActive(key != null);

			if (key != null)
			{
				_actionLabel.SetKey(key);
			}
		}

		private void OnSelected(ContractSpec spec)
		{
			_selected = spec;
			Refresh();
		}

		private void OnAction()
		{
			try
			{
				ContractRun run = _campaign.Contracts.ActiveRun;

				if (run == null)
				{
					_campaign.Contracts.Accept(_selected.Id);
					Refresh();
				}
				else if (run.Phase == ContractPhase.Accepted)
				{
					run.CollectPackage();
					Refresh();
				}
				else if (run.Phase == ContractPhase.PackageCollected)
				{
					run.Depart();
					Navigate(run.Spec.DestinationSceneId);
				}
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
		}

		private async void Navigate(string sceneId)
		{
			_actionButton.interactable = false;

			try
			{
				await _services.Navigator.GoToAsync(sceneId);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
				_actionButton.interactable = true;
			}
		}

		private static bool Contains(IReadOnlyList<ContractSpec> list, ContractSpec spec)
		{
			foreach (ContractSpec item in list)
			{
				if (item == spec)
				{
					return true;
				}
			}

			return false;
		}

		private static string RiskKey(ContractRisk risk)
		{
			return risk switch
			{
				ContractRisk.Low => "risk.low",
				ContractRisk.Medium => "risk.medium",
				_ => "risk.high"
			};
		}

		private static string StateKey(LocationState state)
		{
			return state == LocationState.Blackout ? "location_state.blackout" : "location_state.normal";
		}
	}
}
