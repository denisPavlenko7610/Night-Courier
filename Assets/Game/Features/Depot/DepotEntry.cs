using System;
using System.Collections.Generic;
using System.Linq;
using NightCourier.App;
using NightCourier.Audio;
using NightCourier.Contracts;
using NightCourier.Economy;
using NightCourier.Player;
using RD.Audio;
using RD.DependencyInjection;
using RD.Foundation;
using RD.Mechanics.Interactable;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class DepotEntry : MonoBehaviour, ISceneEntry
	{
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
		[SerializeField] private DepotHudView _hudView;
		[SerializeField] private ContractTerminalView _terminalView;

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

			_player.Begin(services);
			_carry = _player.Carry;

			_hudView.Bind(services.Localization);
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
			_terminalView.Bind(services.Localization);
			_uiSounds.Bind(services.Audio, _terminalView.Panel);
			_ambienceVoice = services.Audio.Play(_ambience);

			_terminal.Interacted += OpenTerminal;
			_van.Interacted += OnVanUsed;
			_carry.HeldChanged += RefreshWorld;
			_terminalView.Selected += OnSelected;
			_terminalView.Accepted += OnAccept;
			_terminalView.CloseRequested += CloseTerminal;

			_hudView.SetCredits(_campaign.Credits);
			ShowRank();

			RefreshWorld();
		}

		private void Update()
		{
			bool covered = _pauseMenu.IsOpen || _upgradeTerminal.IsOpen || _equipmentLocker.IsOpen || _terminalScope != null;

			_hudView.SetHidden(covered);
			_objective.SetHidden(covered);
			_vitalsView.SetHudHidden(covered);
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

			if (_terminalView != null)
			{
				_terminalView.Selected -= OnSelected;
				_terminalView.Accepted -= OnAccept;
				_terminalView.CloseRequested -= CloseTerminal;
			}
		}

		private void ShowRank()
		{
			CourierRank rank = _campaign.Rank;
			string title = _services.Localization.Get(rank.TitleKey);

			if (rank.IsMaxLevel)
			{
				_hudView.SetRank("depot.rank_max", title, rank.TotalExperience);
			}
			else
			{
				_hudView.SetRank("depot.rank", title, rank.ExperienceIntoLevel, rank.ExperienceForNextLevel);
			}
		}

		private void OpenTerminal()
		{
			if (_terminalScope != null)
			{
				return;
			}

			_terminalScope = _services.Input.PushMenu(CloseTerminal);
			_terminalView.Open();
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
			_terminalView.Close();
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
			_hudView.SetCredits(_campaign.Credits);

			ContractRun run = _campaign.Contracts.ActiveRun;

			if (run != null)
			{
				_terminalView.ShowActive(run.Spec);
				return;
			}

			IReadOnlyList<ContractSpec> available = _campaign.Contracts.GetAvailable();

			if (_selected == null || !available.Contains(_selected))
			{
				_selected = available.Count > 0 ? available[0] : null;
			}

			_terminalView.ShowOffers(available, _selected);
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
	}
}
