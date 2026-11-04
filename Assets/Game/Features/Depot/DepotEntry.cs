using System;
using System.Collections.Generic;
using NightCourier.App;
using NightCourier.Audio;
using NightCourier.Contracts;
using NightCourier.Economy;
using NightCourier.Packages;
using NightCourier.Player;
using NightCourier.Weapons;
using RD.Audio;
using RD.DependencyInjection;
using RD.Foundation;
using RD.Mechanics.Interactable;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class DepotEntry : MonoBehaviour, ISceneEntry
	{
		private const string AcceptKey = "contract.accept";
		private const float AmbienceFadeSeconds = 0.5f;

		[SerializeField] private PlayerSession _player;
		[SerializeField] private UiSounds _uiSounds;
		[SerializeField] private AudioCue _ambience;
		[SerializeField] private InteractionPromptView _promptView;
		[SerializeField] private ObjectiveView _objective;
		[SerializeField] private PlayerVitalsView _vitalsView;
		[SerializeField] private WeaponHudView _weaponHud;
		[SerializeField] private PauseMenu _pauseMenu;
		[SerializeField] private Interactable _terminal;
		[SerializeField] private Interactable _van;
		[SerializeField] private UpgradeTerminalView _upgradeTerminal;
		[SerializeField] private EquipmentLockerView _equipmentLocker;
		[SerializeField] private MedkitHudView _medkitHud;
		[SerializeField] private GameObject _packagePrefab;
		[SerializeField] private Transform _packageSpawn;
		[SerializeField] private CanvasGroup _header;
		[SerializeField] private GameObject _terminalPanel;
		[SerializeField] private Button _closeButton;
		[SerializeField] private LocalizedLabel _creditsLabel;
		[SerializeField] private LocalizedLabel _rankLabel;
		[SerializeField] private ContractSlot[] _slots;
		[SerializeField] private GameObject _emptyNotice;
		[SerializeField] private GameObject _detailsRoot;
		[SerializeField] private LocalizedLabel _detailsTitle;
		[SerializeField] private LocalizedLabel _detailsBody;
		[SerializeField] private LocalizedLabel _detailsInfo;
		[SerializeField] private Button _actionButton;
		[SerializeField] private LocalizedLabel _actionLabel;

		private FirstDeliveryTraining _training;
		private GameServices _services;
		private CampaignState _campaign;
		private PlayerCarry _carry;
		private ContractSpec _selected;
		private IDisposable _terminalScope;
		private AudioVoiceHandle _ambienceVoice;

		public void Enter(GameServices services, IObjectResolver scene)
		{
			_campaign = services.Campaign.Current
				?? throw new InvalidOperationException("The depot requires an active campaign.");

			if (_campaign.Contracts.ActiveRun != null)
			{
				UnityLogger.LogWarning("The depot found an unfinished contract and abandoned it.", this);
				_campaign.Contracts.AbandonUnfinished();
			}

			_services = services;

			LocalizedLabel.BindAll(gameObject, services.Localization);

			_player.Begin(services);
			_carry = _player.Carry;

			_promptView.Bind(_player.Interactor, services.Localization, services.Input.Actions);
			_objective.Bind(services.Localization);

			if (_campaign.Contracts.CompletedCount == 0)
			{
				_training = new FirstDeliveryTraining(true);
				_objective.SetTraining(_training);
			}
			_vitalsView.Bind(services.Localization, _player.Vitals);
			_weaponHud.Bind(services.Localization, _player.Weapons);
			_pauseMenu.Bind(services, "pause.abort_contract", OnAbort);
			_upgradeTerminal.Bind(services, _campaign, _player);
			_equipmentLocker.Bind(services, _campaign);
			_medkitHud.Bind(services.Localization, _player.Medkits);
			_uiSounds.Bind(services.Audio);
			_ambienceVoice = services.Audio.Play(_ambience);

			_terminal.Interacted += OpenTerminal;
			_van.Interacted += OnVanUsed;
			_carry.HeldChanged += RefreshWorld;
			_actionButton.onClick.AddListener(OnAccept);
			_closeButton.onClick.AddListener(CloseTerminal);

			_terminalPanel.SetActive(false);
			_creditsLabel.SetKey("depot.credits", _campaign.Credits);
			ShowRank();

			RefreshWorld();
		}

		private void Update()
		{
			bool covered = _pauseMenu.IsOpen || _upgradeTerminal.IsOpen || _equipmentLocker.IsOpen;

			_header.alpha = covered || _terminalScope != null ? 0f : 1f;
			_objective.SetHidden(covered || _terminalScope != null);
			_vitalsView.SetHudHidden(covered || _terminalScope != null);
		}

		private void OnDestroy()
		{
			_training?.Dispose();
			_terminalScope?.Dispose();
			_ambienceVoice.TryStop(AmbienceFadeSeconds);

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

		private void ShowRank()
		{
			CourierRank rank = _campaign.Rank;
			string title = _services.Localization.Get(rank.TitleKey);

			if (rank.IsMaxLevel)
			{
				_rankLabel.SetKey("depot.rank_max", title, rank.TotalExperience);
			}
			else
			{
				_rankLabel.SetKey("depot.rank", title, rank.ExperienceIntoLevel, rank.ExperienceForNextLevel);
			}
		}

		private void OpenTerminal()
		{
			if (_terminalScope != null)
			{
				return;
			}

			_terminalScope = _services.Input.PushMenu(CloseTerminal);
			_terminalPanel.SetActive(true);
			_uiSounds.PlayOpen();

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

			_pauseMenu.SetLeaveAvailable(run != null);

			if (_training != null && _campaign.Contracts.CompletedCount > 0)
			{
				_objective.SetTraining(null);
				_training.Dispose();
				_training = null;
			}

			_van.SetAvailable(run != null && run.Phase == ContractPhase.PackageCollected && carrying);
			_training?.ObserveDepot(run);
			_objective.Show(DepotObjectiveHints.ForDepot(run, carrying));
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
					bool selected = available[index] == _selected;

					_slots[index].Show(
						available[index],
						selected,
						ContractBriefing.SlotLabel(available[index], _services.Localization, selected),
						OnSelected);
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
			_detailsBody.SetRichText(ContractBriefing.Story(spec, _services.Localization));
			_detailsInfo.SetRichText(ContractBriefing.Terms(spec, _services.Localization));
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
			if (_selected == null || _campaign.Contracts.ActiveRun != null)
			{
				return;
			}

			try
			{
				CargoPackage package = SpawnPackage();

				try
				{
					_campaign.Contracts.Accept(_selected.Id);
				}
				catch
				{
					Destroy(package.gameObject);
					throw;
				}
				CloseTerminal();
				RefreshWorld();
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
		}

		private CargoPackage SpawnPackage()
		{
			GameObject instance = Instantiate(_packagePrefab, _packageSpawn.position, _packageSpawn.rotation);

			try
			{
				CargoPackage package = instance.GetComponent<CargoPackage>()
					?? throw new InvalidOperationException("The depot package prefab requires CargoPackage.");

				package.Bind(_carry);
				package.PickedUp += OnPackagePickedUp;
				return package;
			}
			catch
			{
				Destroy(instance);
				throw;
			}
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
				_training?.ObserveDepot(run);

				await _services.Navigator.GoToAsync(run.Spec.DestinationSceneId);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
		}

		private async void OnAbort()
		{
			if (!_campaign.Contracts.Fail())
			{
				return;
			}

			_van.SetAvailable(false);

			try
			{
				await _services.Navigator.GoToAsync(SceneIds.Depot);
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
	}
}
