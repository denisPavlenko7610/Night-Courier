using System;
using System.Collections.Generic;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Player;
using RD.DependencyInjection;
using RD.Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class DepotEntry : MonoBehaviour, ISceneEntry
	{
		private const string AcceptKey = "contract.accept";

		[SerializeField] private PlayerSession _player;
		[SerializeField] private InteractionPromptView _promptView;
		[SerializeField] private ObjectiveView _objective;
		[SerializeField] private PlayerVitalsView _vitalsView;
		[SerializeField] private PauseMenu _pauseMenu;
		[SerializeField] private Interactable _terminal;
		[SerializeField] private Interactable _van;
		[SerializeField] private GameObject _packagePrefab;
		[SerializeField] private Transform _packageSpawn;
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
		private PlayerCarry _carry;
		private ContractSpec _selected;
		private IDisposable _terminalScope;

		public void Enter(GameServices services, IObjectResolver scene)
		{
			_campaign = services.Campaign.Current
				?? throw new InvalidOperationException("The depot requires an active campaign.");

			if (_campaign.Contracts.ActiveRun != null)
			{
				throw new InvalidOperationException("The depot cannot start with an unfinished contract.");
			}

			_services = services;

			LocalizedLabel.BindAll(gameObject, services.Localization);

			_player.Begin(services);
			_carry = _player.Carry;

			_promptView.Bind(_player.Interactor, services.Localization, services.Input.Actions);
			_objective.Bind(services.Localization);
			_vitalsView.Bind(services.Localization, _player.Vitals);
			_pauseMenu.Bind(services);

			_terminal.Interacted += OpenTerminal;
			_van.Interacted += OnVanUsed;
			_carry.HeldChanged += RefreshWorld;
			_actionButton.onClick.AddListener(OnAccept);
			_closeButton.onClick.AddListener(CloseTerminal);

			_terminalPanel.SetActive(false);
			_creditsLabel.SetKey("depot.credits", _campaign.Credits);

			RefreshWorld();
		}

		private void OnDestroy()
		{
			_terminalScope?.Dispose();

			if (_terminal != null)
			{
				_terminal.Interacted -= OpenTerminal;
			}

			if (_van != null)
			{
				_van.Interacted -= OnVanUsed;
			}

			if (_carry != null)
			{
				_carry.HeldChanged -= RefreshWorld;
			}

			_actionButton.onClick.RemoveListener(OnAccept);
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

			RefreshTerminal();
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

		private void RefreshWorld()
		{
			ContractRun run = _campaign.Contracts.ActiveRun;
			bool carrying = _carry.IsCarrying;

			_van.SetAvailable(run != null && run.Phase == ContractPhase.PackageCollected && carrying);
			_objective.Show(ObjectiveHints.ForDepot(run, carrying));
		}

		private void RefreshTerminal()
		{
			_creditsLabel.SetKey("depot.credits", _campaign.Credits);

			ContractRun run = _campaign.Contracts.ActiveRun;

			if (run != null)
			{
				foreach (ContractSlot slot in _slots)
				{
					slot.Hide();
				}

				_emptyNotice.SetActive(false);
				ShowDetails(run.Spec);
				ShowAction(false);
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
			ShowAction(_selected != null);
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

		private void ShowAction(bool visible)
		{
			_actionButton.gameObject.SetActive(visible);

			if (visible)
			{
				_actionLabel.SetKey(AcceptKey);
			}
		}

		private void OnSelected(ContractSpec spec)
		{
			_selected = spec;
			RefreshTerminal();
		}

		private void OnAccept()
		{
			try
			{
				_campaign.Contracts.Accept(_selected.Id);
				SpawnPackage();
				CloseTerminal();
				RefreshWorld();
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
		}

		private void SpawnPackage()
		{
			GameObject instance = Instantiate(_packagePrefab, _packageSpawn.position, _packageSpawn.rotation);
			CargoPackage package = instance.GetComponent<CargoPackage>();

			package.Bind(_carry);
			package.PickedUp += OnPackagePickedUp;
		}

		private void OnPackagePickedUp()
		{
			ContractRun run = _campaign.Contracts.ActiveRun;

			if (run != null && run.Phase == ContractPhase.Accepted)
			{
				run.CollectPackage();
			}

			RefreshWorld();
		}

		private async void OnVanUsed()
		{
			ContractRun run = _campaign.Contracts.ActiveRun;

			if (run == null || run.Phase != ContractPhase.PackageCollected || !_carry.IsCarrying)
			{
				return;
			}

			_van.SetAvailable(false);

			try
			{
				_carry.ConsumeHeld();
				run.Depart();

				await _services.Navigator.GoToAsync(run.Spec.DestinationSceneId);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
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
