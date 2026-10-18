using System;
using System.Linq;
using System.Text;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Packages;
using NightCourier.Player;
using NightCourier.Weapons;
using NightCourier.World;
using RD.Conditions;
using RD.DependencyInjection;
using RD.Foundation;
using RD.Localization;
using RD.Mechanics.ChaseTargetMemory;
using RD.Mechanics.Checkpoint;
using RD.Mechanics.Spawner;
using UnityEngine;
using UnityEngine.UI;

namespace NightCourier.UI
{
	public sealed class WarehouseEntry : MonoBehaviour, ISceneEntry
	{
		private const int LockerAmmoRefill = 8;

		[SerializeField] private PlayerSession _player;
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
		[SerializeField] private GameObject _resultPanel;
		[SerializeField] private LocalizedLabel _resultTitle;
		[SerializeField] private LocalizedLabel _contractTitle;
		[SerializeField] private LocalizedLabel _timerLabel;
		[SerializeField] private Text _breakdownText;
		[SerializeField] private LocalizedLabel _creditsLabel;
		[SerializeField] private Button _returnButton;

		private GameServices _services;
		private CampaignState _campaign;
		private PlayerCarry _carry;
		private ContractRun _run;
		private WarehouseFacts _facts;
		private CargoPackage _package;
		private IDisposable _resultScope;

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
			_pauseMenu.Bind(services, "pause.abort_contract", OnAbort);
			_vitalsView.Bind(services.Localization, _player.Vitals);
			_weaponHud.Bind(services.Localization, _player.Weapons);
			_medkitHud.Bind(services.Localization, _player.Medkits);

			BuildWorld(scene);
			SpawnPackage();

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

			if (_run != null)
			{
				_run.PackageViolationRegistered -= OnPackageViolation;
			}

			if (_package != null)
			{
				_package.Rules.Violated -= OnRuleViolated;
				_package.Integrity.Changed -= OnIntegrityChanged;
				_package.Integrity.Destroyed -= OnPackageDestroyed;
			}

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
			_scanner.Bind(_facts);
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
			_packageHud.Notify("notice.property_damage");
		}

		private void SpawnPackage()
		{
			GameObject instance = Instantiate(_packagePrefab, _packageSpawn.position, _packageSpawn.rotation);

			_package = instance.GetComponent<CargoPackage>();
			_package.Bind(_carry);
			_package.Impact.Arm();
			_package.Rules.Configure(_run.Spec.PackageRules, new PackageRuleContext(_run, _lighting));

			_package.Rules.Violated += OnRuleViolated;
			_run.PackageViolationRegistered += OnPackageViolation;
			_package.Integrity.Changed += OnIntegrityChanged;
			_package.Integrity.Destroyed += OnPackageDestroyed;

			_packageHud.Bind(_services.Localization, _run.Spec.PackageRules);
		}

		private void OnRuleViolated(PackageRuleKind kind)
		{
			_packageHud.Notify("notice.rule_violated", _services.Localization.Get(PackageRuleKeys.NameKey(kind)));
			_run.RegisterViolation();
		}

		private void OnPackageViolation(int count)
		{
			foreach (EscalationEffect effect in _run.Spec.GetEscalation(count))
			{
				switch (effect)
				{
					case EscalationEffect.PackageUnstable:
						_package.Unstable.Activate();
						_packageHud.Notify("notice.package_unstable");
						break;
					case EscalationEffect.SpawnEnemy:
						SpawnAlarmEnemy();
						break;
					case EscalationEffect.FailContract:
						FailContract("result.too_many_violations");
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unknown escalation effect.");
				}
			}
		}

		private void SpawnAlarmEnemy()
		{
			if (!_alarmSpawner.TrySpawn(out GameObject enemy))
			{
				return;
			}

			enemy.GetComponent<ChaseTargetMemory>().ReportTarget(_player.transform);
			_packageHud.Notify("notice.enemy_spawned");
		}

		private void OnIntegrityChanged()
		{
			_packageHud.ShowIntegrity(_package.Integrity.Fraction);
		}

		private void OnPackageDestroyed()
		{
			FailContract("result.package_destroyed");
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

			_resultTitle.SetKey("result.failed_title");
			_breakdownText.text = _services.Localization.Get(reasonKey);
			_creditsLabel.SetKey("depot.credits", _campaign.Credits);

			_resultScope = _services.Input.PushMenu();
			_resultPanel.SetActive(true);
		}

		private void RefreshWorld()
		{
			bool carrying = _carry.IsCarrying;

			_deliveryPoint.SetAvailable(carrying && _facts.CargoScanned && _run.Phase == ContractPhase.InTransit);
			_objective.Show(ObjectiveHints.ForLocation(
				_run,
				new WarehouseProgress(
					carrying,
					_facts.PowerOn,
					_facts.CargoScanned,
					_facts.BatteryRequired,
					_facts.BatteryInserted,
					_player.Heavy.IsCarrying)));
		}

		private async void OnDelivered()
		{
			_deliveryPoint.SetAvailable(false);

			try
			{
				_run.Deliver();
				_carry.ConsumeHeld();

				ContractSettlement settlement = await _campaign.Contracts.SettleAsync();
				await _services.Campaign.SaveAsync();

				ShowResult(settlement);
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
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
