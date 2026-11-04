using System;
using System.Linq;
using NightCourier.AI;
using NightCourier.App;
using NightCourier.Audio;
using NightCourier.Contracts;
using NightCourier.Economy;
using NightCourier.Packages;
using NightCourier.Player;
using NightCourier.Weapons;
using NightCourier.World;
using RD.Conditions;
using RD.DependencyInjection;
using RD.Foundation;
using RD.Mechanics.Checkpoint;
using RD.Mechanics.Interactable;
using RD.Mechanics.Spawner;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class WarehouseEntry : MonoBehaviour, ISceneEntry
	{
		private const int LockerAmmoRefill = 8;
		private const string ScanFirstKey = "notice.scan_first";
		private const string ScanInterruptedKey = "notice.scan_interrupted";
		private const string SettlementFailedKey = "result.settlement_failed";

		[SerializeField] private PlayerSession _player;
		[SerializeField] private WarehouseAudio _sounds;
		[SerializeField] private UiSounds _uiSounds;
		[SerializeField] private InteractionPromptView _promptView;
		[SerializeField] private ObjectiveView _objective;
		[SerializeField] private PauseMenu _pauseMenu;
		[SerializeField] private Interactable _deliveryPoint;
		[SerializeField] private GameObject _packagePrefab;
		[SerializeField] private Transform _packageSpawn;
		[SerializeField] private PowerLever _powerLever;
		[SerializeField] private FreightLift _freightLift;
		[SerializeField] private CargoScanner _scanner;
		[SerializeField] private ConditionDoor _maintenanceDoor;
		[SerializeField] private ConditionDoor _scannerHatch;
		[SerializeField] private BreakableBarricade _barricade;
		[SerializeField] private BatteryCell _battery;
		[SerializeField] private GeneratorSocket _generator;
		[SerializeField] private PlayerVitalsView _vitalsView;
		[SerializeField] private WeaponHudView _weaponHud;
		[SerializeField] private MedkitHudView _medkitHud;
		[SerializeField] private CheckpointSet _checkpoints;
		[SerializeField] private CourierLocker[] _lockers;
		[SerializeField] private EncounterGroup[] _encounters;
		[SerializeField] private Spawner _alarmSpawner;
		[SerializeField] private WarehouseLighting _lighting;
		[SerializeField] private PackageHudView _packageHud;
		[SerializeField] private CanvasGroup _hud;
		[SerializeField] private GameObject _resultPanel;
		[SerializeField] private LocalizedLabel _resultTitle;
		[SerializeField] private LocalizedLabel _contractTitle;
		[SerializeField] private LocalizedLabel _timerLabel;
		[SerializeField] private ResultPanelView _resultView;
		[SerializeField] private LocalizedLabel _creditsLabel;
		[SerializeField] private Button _returnButton;

		private FirstDeliveryTraining _training;
		private GameServices _services;
		private CampaignState _campaign;
		private PlayerCarry _carry;
		private ContractRun _run;
		private WarehouseFacts _facts;
		private CargoPackage _package;
		private IDisposable _resultScope;
		private PackageWatch _packageWatch;
		private EscalationResponder _escalation;
		private bool _wasScanning;
		private bool _wasOnPlate;

		public void Enter(GameServices services, IObjectResolver scene)
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
			_carry = _player.Carry;

			_promptView.Bind(_player.Interactor, services.Localization, services.Input.Actions);
			_objective.Bind(services.Localization);

			if (_run.Spec.Id == FirstDeliveryTraining.ContractId)
			{
				_training = new FirstDeliveryTraining(false);
				_objective.SetTraining(_training);
			}
			_pauseMenu.Bind(services, "pause.abort_contract", OnAbort);
			_vitalsView.Bind(services.Localization, _player.Vitals);
			_weaponHud.Bind(services.Localization, _player.Weapons);
			_medkitHud.Bind(services.Localization, _player.Medkits);

			BuildWorld(scene);
			_sounds.Bind(services.Audio, _facts);
			_uiSounds.Bind(services.Audio);
			SpawnPackage();
			AnnounceRules();

			_scanner.Bind(_facts, _package);
			_scanner.ScanInterrupted += OnScanInterrupted;

			_deliveryPoint.Interacted += OnDelivered;
			_carry.HeldChanged += RefreshWorld;
			_carry.PickUpRefused += OnHandsFull;
			_player.Heavy.HeldChanged += RefreshWorld;
			_player.Heavy.GrabRefused += OnHandsFull;
			_player.Weapons.UseRefused += OnWeaponRefused;
			_player.Weapons.ShotFired += OnShotFired;
			_facts.Changed += RefreshWorld;
			_barricade.Broken += OnBarricadeBroken;
			_returnButton.onClick.AddListener(OnReturn);

			_resultPanel.SetActive(false);

			RefreshWorld();
		}

		private void Update()
		{
			bool covered = _pauseMenu.IsOpen || _resultPanel.activeSelf;

			_hud.alpha = covered ? 0f : 1f;
			_objective.SetHidden(covered);
			_vitalsView.SetHudHidden(covered);

			if (_run == null || _run.IsFinished)
			{
				return;
			}

			_run.Tick(Time.deltaTime);

			if (_run.Phase != ContractPhase.InTransit)
			{
				return;
			}

			if (_run.HasDeadline)
			{
				_timerLabel.SetKey("warehouse.time_left", ClockText.Format(_run.RemainingSeconds));
			}

			if (_scanner.IsScanning != _wasScanning || _scanner.HasPackageOnPlate != _wasOnPlate)
			{
				_wasScanning = _scanner.IsScanning;
				_wasOnPlate = _scanner.HasPackageOnPlate;
				RefreshWorld();
			}
		}

		private void OnDestroy()
		{
			_training?.Dispose();
			_resultScope?.Dispose();

			if (_deliveryPoint != null)
			{
				_deliveryPoint.Interacted -= OnDelivered;
			}

			if (_carry != null)
			{
				_carry.HeldChanged -= RefreshWorld;
				_carry.PickUpRefused -= OnHandsFull;
			}

			if (_player != null && _player.Heavy != null)
			{
				_player.Heavy.HeldChanged -= RefreshWorld;
				_player.Heavy.GrabRefused -= OnHandsFull;
				_player.Weapons.UseRefused -= OnWeaponRefused;
				_player.Weapons.ShotFired -= OnShotFired;
			}

			if (_player != null && _player.Vitals != null)
			{
				_player.Vitals.Died -= OnPlayerDied;
			}

			if (_checkpoints != null)
			{
				_checkpoints.Activated -= OnCheckpointActivated;
			}

			if (_facts != null)
			{
				_facts.Changed -= RefreshWorld;
			}

			if (_barricade != null)
			{
				_barricade.Broken -= OnBarricadeBroken;
			}

			if (_scanner != null)
			{
				_scanner.ScanInterrupted -= OnScanInterrupted;
			}

			_packageWatch?.Dispose();
			_escalation?.Dispose();
			_returnButton.onClick.RemoveListener(OnReturn);
		}

		private void BuildWorld(IObjectResolver scene)
		{
			bool blackout = _run.Spec.LocationState == LocationState.Blackout;

			_facts = scene.Resolve<WarehouseFacts>();
			_facts.SetBatteryRequired(blackout);

			ConditionEngine engine = scene.Resolve<ConditionEngine>();

			_battery.gameObject.SetActive(blackout);

			if (blackout)
			{
				_battery.Bind(_player.Heavy);
			}

			_generator.Bind(_facts, _player.Heavy);
			_powerLever.Bind(_facts);
			_freightLift.Bind(_facts);
			_barricade.Bind(_player.GetComponent<CharacterController>());
			_maintenanceDoor.Bind(_facts, engine, WarehouseConditions.MaintenanceDoor());
			_scannerHatch.Bind(_facts, engine, WarehouseConditions.ScannerHatch());
			_lighting.Bind(_run.Spec.LocationState, _facts);

			foreach (CourierLocker locker in _lockers)
			{
				locker.Bind(_checkpoints);
			}

			foreach (EncounterGroup encounter in _encounters)
			{
				encounter.gameObject.SetActive(_run.Spec.Encounters.Contains(encounter.Kind));

				foreach (EnemyAlert alert in encounter.GetComponentsInChildren<EnemyAlert>(true))
				{
					alert.Bind(_player.transform);
				}

				foreach (EnemyAudio enemySounds in encounter.GetComponentsInChildren<EnemyAudio>())
				{
					enemySounds.Bind(_services.Audio);
				}
			}

			ApplySpawn(_checkpoints.ActiveCheckpoint);

			_checkpoints.Activated += OnCheckpointActivated;
			_player.Vitals.Died += OnPlayerDied;
		}

		private void ApplySpawn(Checkpoint checkpoint)
		{
			_player.Vitals.SetSpawn(new Pose(checkpoint.SpawnPosition, checkpoint.SpawnRotation));
		}

		private void OnCheckpointActivated(Checkpoint checkpoint)
		{
			ApplySpawn(checkpoint);
			_player.Weapons.RefillAmmo(LockerAmmoRefill);
			_sounds.PlayCheckpoint();
			_packageHud.Notify("notice.checkpoint");
		}

		private void OnPlayerDied()
		{
			_run.RegisterDeath();
		}

		private void OnWeaponRefused(WeaponKind kind)
		{
			_packageHud.Notify("notice.weapon_blocked", _services.Localization.Get(WeaponRules.NameKey(kind)));
		}

		private void OnShotFired()
		{
			_run.RegisterShot();
		}

		private void OnHandsFull()
		{
			_packageHud.Notify("notice.hands_full");
		}

		private void OnBarricadeBroken()
		{
			_run.RegisterPropertyDamage();
			_sounds.PlayBarricadeBreak(_barricade.transform.position);
			_packageHud.Notify("notice.property_damage");
		}

		private void SpawnPackage()
		{
			GameObject instance = Instantiate(_packagePrefab, _packageSpawn.position, _packageSpawn.rotation);

			_package = instance.GetComponent<CargoPackage>();
			_package.Bind(_carry);
			_package.Impact.Arm();
			_package.Rules.Configure(_run.Spec.PackageRules, new PackageRuleContext(_run, _lighting));

			_packageHud.Bind(_services.Localization, _run.Spec.PackageRules);

			_packageWatch = new PackageWatch(_run, _package, _packageHud, _sounds, _services.Localization, FailContract);
			_escalation = new EscalationResponder(
				_run,
				_package,
				_player,
				_alarmSpawner,
				_packageHud,
				_sounds,
				_services.Audio,
				FailContract);
		}

		private void OnScanInterrupted()
		{
			if (_run.Phase == ContractPhase.InTransit)
			{
				_packageHud.Notify(ScanInterruptedKey);
			}
		}

		private void AnnounceRules()
		{
			foreach (PackageRuleKind kind in _run.Spec.PackageRules)
			{
				_packageHud.Announce(PackageRuleKeys.HintKey(kind));
			}
		}

		private void FailContract(string reasonKey)
		{
			if (_run.Phase != ContractPhase.InTransit)
			{
				return;
			}

			_deliveryPoint.SetAvailable(false);
			_campaign.Contracts.Fail();
			_objective.Show(null);

			if (_carry.IsCarrying)
			{
				_carry.ConsumeHeld();
			}
			else if (_package != null)
			{
				Destroy(_package.gameObject);
			}

			ShowFailure(reasonKey);
		}

		private void ShowFailure(string reasonKey)
		{
			_sounds.PlayFailed();
			_resultTitle.SetKey("result.failed_title");
			_resultView.ShowFailure(_services.Localization, _run.Spec, reasonKey);
			_creditsLabel.SetKey("depot.credits", _campaign.Credits);

			_resultScope ??= _services.Input.PushMenu();
			_resultPanel.SetActive(true);
		}

		private void RefreshWorld()
		{
			bool carrying = _carry.IsCarrying;
			bool inTransit = _run.Phase == ContractPhase.InTransit;

			_deliveryPoint.SetAvailable(carrying && inTransit);
			_training?.ObserveLocation(new WarehouseProgress(carrying, _facts.PowerOn, _facts.CargoScanned), _run.Phase == ContractPhase.Delivered);
			_objective.Show(WarehouseObjectiveHints.ForLocation(
				_run,
				new WarehouseProgress(
					carrying,
					_facts.PowerOn,
					_facts.CargoScanned,
					_facts.BatteryRequired,
					_facts.BatteryInserted,
					_player.Heavy.IsCarrying,
					_scanner.IsScanning,
					_scanner.HasPackageOnPlate)));
		}

		private async void OnDelivered()
		{
			if (_run.Phase != ContractPhase.InTransit || !_carry.IsCarrying)
			{
				return;
			}

			if (!_facts.CargoScanned)
			{
				_packageHud.Notify(ScanFirstKey);
				return;
			}

			_deliveryPoint.SetAvailable(false);

			try
			{
				_run.RecordCargoIntegrity(_package.Integrity.Fraction);

				if (!_run.Deliver())
				{
					return;
				}

				_carry.ConsumeHeld();

				ContractOutcome outcome = _run.CaptureOutcome();
				ContractSettlement settlement = await _campaign.Contracts.SettleAsync();
				RankChange rankChange = _campaign.Rank.Apply(settlement.Rating);
				bool saved = await TrySaveAsync();

				if (this == null)
				{
					return;
				}

				ShowResult(settlement, outcome, rankChange, saved);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);

				if (this != null)
				{
					_campaign.Contracts.AbandonUnfinished();
					ShowFailure(SettlementFailedKey);
				}
			}
		}

		private async Awaitable<bool> TrySaveAsync()
		{
			try
			{
				await _services.Campaign.SaveAsync();
				return true;
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
				return false;
			}
		}

		private void OnAbort()
		{
			if (_run.Phase == ContractPhase.Delivered)
			{
				return;
			}

			_campaign.Contracts.Fail();
			Navigate(SceneIds.Depot);
		}

		private void OnReturn()
		{
			Navigate(SceneIds.Depot);
		}

		private void ShowResult(ContractSettlement settlement, ContractOutcome outcome, RankChange rankChange, bool saved)
		{
			_sounds.PlayDelivered();
			_resultView.ShowSuccess(_services.Localization, _run.Spec, settlement, outcome, _campaign.Rank, rankChange, saved);
			_creditsLabel.SetKey("depot.credits", _campaign.Credits);

			_resultScope ??= _services.Input.PushMenu();
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
	}
}
