#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NightCourier.AI;
using NightCourier.Contracts;
using NightCourier.Packages;
using NightCourier.Player;
using NightCourier.Presentation;
using NightCourier.UI;
using NightCourier.World;
using NUnit.Framework;
using RD.Mechanics.Combat;
using RD.Mechanics.Interactable;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace NightCourier.PlayModeTests
{
	[Category("Routes")]
	public sealed class RealInputPlayModeTests
	{
		private const int TestTimeoutMilliseconds = 300000;
		private const string LightsOut = "contract.lights-out";
		private const string Express = "contract.express";
		private const string Fragile = "contract.fragile";
		private const string RestrictedCargo = "contract.restricted-cargo";
		private const long RestrictedCargoPay = 450;
		private const long CleanDeliveryBonus = 100;
		private const long ExpressPay = 320;
		private const long OnTimeBonus = 75;
		private const string ReportFile = "Temp/RealInputRoutes.txt";
		private const string CueFolder = "Temp/CueShots";
		private const float PlatformCeilingY = 1.35f;
		private const float MaxHoldDistance = 1.5f;
		private const float ShotsTimeoutSeconds = 6f;
		private const float PistolClickGapSeconds = 0.3f;
		private const float EnemyApproachSeconds = 12f;
		private const float EnemyKillSeconds = 10f;
		private const float DeathObservationSeconds = 1.5f;
		private const float DoorTimeoutSeconds = 6f;
		private const float LiftTimeoutSeconds = 8f;
		private const float ScanTimeoutSeconds = 18f;
		private const float DeliveryTimeoutSeconds = 12f;
		private const float TerminalAimHeightFraction = 0.6f;

		private static readonly Vector3 LeverStand = new(-18.6f, 0f, -7.2f);
		private static readonly Vector3 DoorApproach = new(-18f, 0f, -5.4f);
		private static readonly Vector3 DoorInner = new(-18f, 0f, -4.3f);
		private static readonly Vector3 DoorOuter = new(-18f, 0f, -2.3f);
		private static readonly Vector3 LiftApproach = new(12.2f, 0f, 9f);
		private static readonly Vector3 LiftStand = new(12.2f, 0f, 10.7f);
		private static readonly Vector3 ScannerApproach = new(14.4f, 1.5f, 11f);
		private static readonly Vector3 BarricadeInner = new(-12.4f, 0f, -8f);
		private static readonly Vector3 BarricadeOuter = new(-10.6f, 0f, -8f);
		private static readonly Vector3 HatchOuter = new(17f, 1.5f, 11f);
		private static readonly Vector3 TerminalStand = new(18.5f, 1.5f, 11f);
		private static readonly Vector3 CrawlerArena = new(-4f, 0f, -9.4f);
		private static readonly Vector3 GeneratorStand = new(-18.4f, 0f, -10.5f);

		private readonly List<EnemyStateLog> _enemies = new();
		private int _minHealth;
		private bool _captureCues;
		private bool _sprint = true;
		private GameDriver _driver;
		private InputBot _bot;
		private WarehouseFacts _facts;
		private PlayerSession _player;
		private readonly List<string> _marks = new();
		private float _startTime;

		[SetUp]
		public void DeleteTestSave()
		{
			GameDriver.DeleteTestSaves();

			_driver = new GameDriver();
			_marks.Clear();
			_captureCues = false;
			_sprint = true;
		}

		[TearDown]
		public void DisposeBot()
		{
			_bot?.Dispose();
			_bot = null;
		}

		[OneTimeTearDown]
		public void DeleteTestSaveAfterTheRun()
		{
			GameDriver.DeleteTestSaves();
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator StandardDelivery_DockToDelivery_WithKeyboardAndMouseEvents()
		{
			yield return _driver.StartContract(GameDriver.StandardDelivery);
			yield return Begin();
			yield return PickUpPackage();
			yield return PullLever();
			yield return SafeRoute();
			yield return DeliverAtScanner();

			Finish("StandardDelivery");

			Assert.That(_driver.ResultText, Does.Contain("Base pay: 150"));
			Assert.That(_driver.ResultText, Does.Not.Contain("Property damage: -"));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator StandardDelivery_ScannerAndDeliveryCues_AreCapturedFromTheApproach()
		{
			_captureCues = true;

			yield return _driver.StartContract(GameDriver.StandardDelivery);
			yield return Begin();

			yield return PickUpPackage();

			yield return CueShot("0a_just_picked_up");
			yield return PullLever();
			yield return CueShot("0b_after_lever");
			yield return SafeRoute();
			yield return DeliverAtScanner();

			Finish("StandardDelivery cue captures");

			Assert.That(Directory.GetFiles(CueFolder, "*.png").Length, Is.GreaterThanOrEqualTo(5));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator LightsOut_BatteryRoute_WithKeyboardAndMouseEvents()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.AcceptInTerminal(LightsOut);
			yield return _driver.CarryPackageToTheVanAndDepart();
			yield return Begin();

			Assert.That(_facts.BatteryRequired, Is.True);

			yield return FetchBatteryAndPower();
			Mark("power restored");
			yield return PickUpPackage();
			yield return SafeRoute();
			yield return DeliverAtScanner();

			Finish("LightsOut");

			Assert.That(_driver.ResultText, Does.Contain("Base pay: 260"));
			Assert.That(_driver.ResultText, Does.Not.Contain("Property damage: -"));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator RestrictedCargo_LiveThreats_BatteryRouteAndCleanSettlement()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();

			foreach (string prerequisite in new[] { GameDriver.StandardDelivery, Fragile, LightsOut, Express })
			{
				yield return _driver.CompleteInState(prerequisite);
			}

			yield return _driver.AcceptInTerminal(RestrictedCargo);
			yield return _driver.CarryPackageToTheVanAndDepart();
			yield return Begin();

			Assert.That(_enemies.Exists(log => log.Enemy.GetComponent<CrawlerBrain>() != null), Is.True);
			Assert.That(_enemies.Exists(log => log.Enemy.GetComponent<SecurityDroneBrain>() != null), Is.True);

			yield return FetchBatteryAndPower();
			yield return PickUpPackage();
			yield return SafeRoute();
			yield return DeliverAtScanner();

			Finish("Restricted Cargo with live threats");

			Assert.That(_driver.ResultText, Does.Contain("Base pay: " + RestrictedCargoPay));
			Assert.That(_driver.ResultText, Does.Contain("No violations: " + CleanDeliveryBonus));
			Assert.That(_driver.ResultText, Does.Contain("Rule violations: 0"));
			Assert.That(_driver.ResultText, Does.Not.Contain("Property damage: -"));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);

			long credits = _driver.Campaign.Credits;
			yield return _driver.ReturnToDepotFromResult();
			yield return _driver.ReturnToMainMenu();
			yield return _driver.ContinueToDepot();
			Assert.That(_driver.Campaign.Credits, Is.EqualTo(credits));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Express_SafeRoute_Sprinting_MakesTheOnTimeBonus()
		{
			yield return StartExpress();
			yield return Begin();
			yield return PickUpPackage();
			yield return PullLever();
			yield return SafeRoute();
			yield return DeliverAtScanner();

			Finish("Express delivery, safe route, sprinting");

			AssertOnTime(true);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Express_ShortRoute_Sprinting_MakesTheOnTimeBonus()
		{
			yield return StartExpress();
			yield return Begin();
			yield return PickUpPackage();
			yield return PullLever();
			yield return ShortRoute();
			yield return DeliverAtScanner();

			Finish("Express delivery, short route, sprinting");

			AssertOnTime(true);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Express_ShortRoute_AfterAFight_LosesTheOnTimeBonus()
		{
			yield return StartExpress();
			yield return Begin();
			yield return PickUpPackage();
			yield return PullLever();
			yield return BreakBarricade();
			yield return _bot.Through(BarricadeOuter, sprint: true);
			yield return _bot.GoTo(CrawlerArena, sprint: true, arrive: 0.8f);
			yield return FightTheCrawlers(_enemies.FindAll(log => log.Enemy.GetComponent<CrawlerBrain>() != null));
			yield return _bot.GoTo(ScannerApproach, sprint: true, timeoutSeconds: 45f, arrive: 0.8f);
			yield return DeliverAtScanner();

			Finish("Express delivery, short route, after fighting both crawlers");

			AssertOnTime(false);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Express_SafeRoute_Walking_StillMeetsTheDeadline()
		{
			_sprint = false;

			yield return StartExpress();
			yield return Begin();
			yield return PickUpPackage();
			yield return PullLever();
			yield return SafeRoute();
			yield return DeliverAtScanner();

			Finish("Express delivery, safe route, walking");

			Assert.That(_driver.ResultText, Does.Contain("Base pay: " + ExpressPay));
			Assert.That(_driver.ResultText, Does.Not.Contain("The deadline passed"), "A walking courier missed the deadline.");
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Express_Crawlers_ShowWalkAttackHitAndTurnOff_UnderRealInput()
		{
			yield return StartExpress();
			yield return Begin();
			yield return BreakBarricade();
			yield return _bot.Through(BarricadeOuter, sprint: true);
			yield return _bot.GoTo(CrawlerArena, sprint: true, arrive: 0.8f);

			List<EnemyStateLog> crawlers = _enemies.FindAll(log => log.Enemy.GetComponent<CrawlerBrain>() != null);

			Assert.That(crawlers.Count, Is.EqualTo(2), "Express should field two crawlers.");

			yield return FightTheCrawlers(crawlers);

			Finish("Express crawlers under real input");

			foreach (string state in new[] { "Walk", "Attack", "Hit", "TurnOff" })
			{
				Assert.That(crawlers.Exists(log => log.Saw(state)), Is.True, "No crawler showed the " + state + " state.");
			}
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Express_Drone_ShowsAttackAndHit_UnderRealInput()
		{
			yield return StartExpress();
			yield return Begin();
			yield return PullLever();
			yield return _bot.GoTo(DoorApproach, sprint: true, arrive: 0.5f);
			yield return _bot.Through(DoorInner, sprint: true);
			yield return GameDriver.WaitUntil(() => FindDoor("MaintenanceDoor").IsOpen, "the maintenance door", DoorTimeoutSeconds);
			yield return _bot.Through(DoorOuter, sprint: true);

			EnemyStateLog drone = _enemies.Find(log => log.Enemy.GetComponent<SecurityDroneBrain>() != null);

			Assert.That(drone, Is.Not.Null, "Express should field a security drone.");

			EnemyAnimation target = drone.Enemy;
			HealthCarrier health = GameDriver.Get<HealthCarrier>(target, "_health");

			yield return _bot.Tap(Key.Digit1);
			yield return _bot.Engage(
				() => target != null && health.Pool.State.IsAlive ? target.GetComponentInChildren<Collider>().bounds.center : null,
				() => target == null || !health.Pool.State.IsAlive,
				EnemyApproachSeconds + EnemyKillSeconds,
				PistolClickGapSeconds);

			Mark("drone down");

			yield return _bot.Wait(DeathObservationSeconds);

			Finish("Express drone under real input");

			Assert.That(target == null || !health.Pool.State.IsAlive, Is.True, "The drone was not shot down.");
			Assert.That(drone.Saw("Attack"), Is.True, "The drone never showed the Attack state.");
			Assert.That(drone.Saw("Hit"), Is.True, "The drone never showed the Hit state.");
		}

		private IEnumerator FightTheCrawlers(List<EnemyStateLog> crawlers)
		{
			float wait = Time.time + EnemyApproachSeconds;

			while (Time.time < wait && !crawlers.Exists(log => log.Saw("Attack")))
			{
				yield return _bot.Frame();
			}

			Mark("crawlers engaged");

			foreach (EnemyStateLog log in crawlers)
			{
				EnemyAnimation target = log.Enemy;
				HealthCarrier health = GameDriver.Get<HealthCarrier>(target, "_health");

				yield return _bot.Engage(
					() => target != null && health.Pool.State.IsAlive ? target.GetComponentInChildren<Collider>().bounds.center : null,
					() => target == null || !health.Pool.State.IsAlive,
					EnemyKillSeconds,
					PistolClickGapSeconds);

				Mark("crawler down");

				yield return _bot.Wait(DeathObservationSeconds);
			}
		}

		private void AssertOnTime(bool expectedBonus)
		{
			Assert.That(_driver.ResultText, Does.Contain("Base pay: " + ExpressPay));
			Assert.That(_driver.ResultText, Does.Not.Contain("The deadline passed"), "The deadline passed.");

			if (expectedBonus)
			{
				Assert.That(_driver.ResultText, Does.Contain("On time: " + OnTimeBonus), "The on-time bonus was not paid.");
			}
			else
			{
				Assert.That(_driver.ResultText, Does.Not.Contain("On time: "), "The on-time bonus was paid.");
			}
		}

		private IEnumerator StartExpress()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.CompleteInState(Fragile);
			yield return _driver.CompleteInState(LightsOut);
			yield return _driver.AcceptInTerminal(Express);
			yield return _driver.CarryPackageToTheVanAndDepart();
		}

		private IEnumerator Begin()
		{
			_player = _driver.Player;
			_facts = GameDriver.Get<WarehouseFacts>(_driver.Warehouse, "_facts");
			_bot = new InputBot(_player, GameDriver.Get<float>(_player.GetComponent<PlayerMotor>(), "_sensitivity"));
			_enemies.Clear();
			_minHealth = int.MaxValue;

			foreach (EnemyAnimation enemy in Object.FindObjectsByType<EnemyAnimation>(FindObjectsInactive.Exclude))
			{
				_enemies.Add(new EnemyStateLog(enemy));
			}

			_bot.FrameHook = SampleEnemies;

			yield return _bot.Wait(0.5f);

			_startTime = Time.time;
			_marks.Add("start at " + _bot.Position.ToString("F1"));
		}

		private void SampleEnemies()
		{
			_minHealth = Mathf.Min(_minHealth, _player.Vitals.Current);

			foreach (EnemyStateLog log in _enemies)
			{
				log.Sample(_bot.Position);
			}
		}

		private void Mark(string name)
		{
			_marks.Add($"{Time.time - _startTime,6:F1} s  {name}  at {_bot.Position:F1}");
		}

		private void Finish(string title)
		{
			_marks.Insert(0, $"== {title}: {Time.time - _startTime:F1} s, stuck count {_bot.StuckCount}, health {_player.Vitals.Current} (min {_minHealth}), violations {(_driver.Run != null ? _driver.Run.ViolationCount : -1)}");

			foreach (EnemyStateLog log in _enemies)
			{
				_marks.Add(log.Describe());
			}

			File.AppendAllText(ReportFile, string.Join("\n", _marks) + "\n");
		}

		private IEnumerator CueShot(string name)
		{
			if (!_captureCues)
			{
				yield break;
			}

			yield return _bot.Wait(0.3f);

			ObjectiveView objective = Object.FindFirstObjectByType<ObjectiveView>();

			GameDriver.Get<VisualElement>(objective, "_plate").style.display = DisplayStyle.None;

			RD.Mechanics.PickupDrop.PickupItem carried = _player.Carry.Held;

			if (carried != null)
			{
				Camera view = _player.GetComponentInChildren<Camera>();
				Vector3 local = view.transform.InverseTransformPoint(carried.transform.position);
				Vector3 viewport = view.WorldToViewportPoint(carried.transform.position);
				bool drawn = false;

				foreach (Renderer renderer in carried.GetComponentsInChildren<Renderer>())
				{
					drawn |= renderer.enabled && renderer.gameObject.activeInHierarchy;
				}

				Rigidbody body = carried.GetComponent<Rigidbody>();

				_marks.Add($"   {name}: carried package world {carried.transform.position:F2}, camera {view.transform.position:F2}, camera space {local:F2}, viewport {viewport:F2}, renderer on {drawn}, kinematic {body != null && body.isKinematic}, layer {LayerMask.LayerToName(carried.gameObject.layer)}, parent {(carried.transform.parent != null ? carried.transform.parent.name : "none")}");
			}

			yield return new WaitForEndOfFrame();

			Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();

			Directory.CreateDirectory(CueFolder);
			File.WriteAllBytes(Path.Combine(CueFolder, name + ".png"), shot.EncodeToPNG());
			Object.Destroy(shot);
		}

		private void AssertTheHandHoldsThePackage(string where)
		{
			RD.Mechanics.PickupDrop.PickupItem held = _player.Carry.Held;

			Assert.That(held, Is.Not.Null, "The package is not in hand " + where);

			Camera view = _player.GetComponentInChildren<Camera>();
			float distance = (held.transform.position - view.transform.position).magnitude;

			Assert.That(distance, Is.LessThan(MaxHoldDistance), $"The carried package drifted {distance:F1} m from the camera {where}");
		}

		private IEnumerator PickUpPackage()
		{
			CargoPackage package = Object.FindFirstObjectByType<CargoPackage>();

			yield return _bot.GoTo(package.transform.position, sprint: _sprint, arrive: 1.4f);
			yield return _bot.Interact(package.GetComponent<Interactable>(), "the package");
			yield return GameDriver.WaitUntil(() => _player.Carry.IsCarrying, "the package in hand", 3f);

			Mark("package in hand");
		}

		private IEnumerator PullLever()
		{
			Interactable lever = GameDriver.Get<Interactable>(Object.FindFirstObjectByType<PowerLever>(), "_interactable");

			yield return _bot.GoTo(LeverStand, sprint: _sprint);
			yield return _bot.Interact(lever, "the breaker panel");

			Assert.That(_facts.PowerOn, Is.True, "The breaker panel did not switch the power on.");

			Mark("power on");

			if (_player.Carry.IsCarrying)
			{
				AssertTheHandHoldsThePackage("after the sprint to the breaker panel");
			}
		}

		private IEnumerator FetchBatteryAndPower()
		{
			Interactable battery = GameDriver.Get<Interactable>(Object.FindFirstObjectByType<BatteryCell>(), "_interactable");
			Interactable generator = GameDriver.Get<Interactable>(Object.FindFirstObjectByType<GeneratorSocket>(), "_interactable");

			yield return _bot.GoTo(battery.transform.position + Vector3.back * 1.1f, sprint: _sprint, arrive: 0.5f);
			yield return _bot.Interact(battery, "the battery");
			yield return GameDriver.WaitUntil(() => _player.Heavy.IsCarrying, "the battery in both hands", 3f);

			Mark("battery in hands");

			yield return _bot.GoTo(GeneratorStand, sprint: false, arrive: 0.5f);
			yield return _bot.Interact(generator, "the generator");

			Assert.That(_facts.BatteryInserted, Is.True, "The generator did not take the battery.");

			Mark("battery inserted");

			yield return PullLever();
		}

		private IEnumerator SafeRoute()
		{
			ConditionDoor door = FindDoor("MaintenanceDoor");
			FreightLift lift = Object.FindFirstObjectByType<FreightLift>();
			Interactable console = GameDriver.Get<Interactable>(lift, "_console");

			yield return _bot.GoTo(DoorApproach, sprint: _sprint, arrive: 0.5f);
			yield return _bot.Through(DoorInner, sprint: _sprint);

			float doorWait = Time.time;

			yield return GameDriver.WaitUntil(() => door.IsOpen, "the maintenance door", DoorTimeoutSeconds);

			if (Time.time - doorWait > 0.05f)
			{
				Mark($"waited {Time.time - doorWait:F1} s for the maintenance door");
			}

			yield return _bot.Through(DoorOuter, sprint: _sprint);

			Mark("through the maintenance door");

			yield return _bot.GoTo(LiftApproach, sprint: _sprint);

			if (_captureCues)
			{
				yield return _bot.Aim(new Vector3(10.4f, 1.6f, 11f));
				yield return CueShot("0c_lift_screen");
			}

			yield return _bot.Through(LiftStand, sprint: false, arrive: 0.3f);
			yield return _bot.Interact(console, "the freight lift console");
			yield return GameDriver.WaitUntil(() => lift.PlatformTransform.position.y > PlatformCeilingY, "the lift at the top", LiftTimeoutSeconds);

			Mark("lift at the top");

			AssertTheHandHoldsThePackage("at the top of the lift");

			yield return CueShot("1_lift_top_carrying");

			yield return _bot.Through(ScannerApproach, sprint: _sprint);

			Mark("at the scanner approach");

			yield return CueShot("2_scanner_approach_carrying");
		}

		private IEnumerator BreakBarricade()
		{
			BreakableBarricade barricade = Object.FindFirstObjectByType<BreakableBarricade>();
			bool broken = false;

			barricade.Broken += () => broken = true;

			yield return _bot.GoTo(BarricadeInner, sprint: _sprint, arrive: 0.5f);

			yield return _bot.Tap(Key.Digit1);
			yield return _bot.Aim(barricade.GetComponent<Collider>().bounds.center);

			float end = Time.time + ShotsTimeoutSeconds;

			while (!broken && Time.time < end)
			{
				yield return _bot.Click();
				yield return _bot.Wait(PistolClickGapSeconds);
			}

			Assert.That(broken, Is.True, "The pistol shots did not break the barricade.");

			Mark("barricade broken");
		}

		private IEnumerator ShortRoute()
		{
			yield return BreakBarricade();
			yield return _bot.Through(BarricadeOuter, sprint: _sprint);
			yield return _bot.GoTo(ScannerApproach, sprint: _sprint, timeoutSeconds: 45f, arrive: 0.8f);

			Mark("at the scanner approach");
		}

		private IEnumerator DeliverAtScanner()
		{
			CargoScanner scanner = Object.FindFirstObjectByType<CargoScanner>();
			Collider plate = GameDriver.Get<Component>(scanner, "_plate").GetComponentInChildren<Collider>();
			CargoPackage package = Object.FindFirstObjectByType<CargoPackage>();
			ConditionDoor hatch = FindDoor("ScannerHatch");

			yield return _bot.Aim(plate.bounds.center);
			yield return _bot.Tap(Key.G);

			Mark("package set down");

			yield return _bot.Wait(0.6f);
			yield return CueShot("3_scanner_scanning");

			yield return GameDriver.WaitUntil(() => _facts.CargoScanned, "the cargo scan", ScanTimeoutSeconds);

			Mark("cargo scanned");

			yield return CueShot("4_scanner_done");

			yield return _bot.Interact(package.GetComponent<Interactable>(), "the scanned package");
			yield return GameDriver.WaitUntil(() => _player.Carry.IsCarrying, "the package picked up again", 3f);
			yield return GameDriver.WaitUntil(() => hatch.IsOpen, "the scanner hatch", DoorTimeoutSeconds);
			yield return _bot.Through(HatchOuter, sprint: false);
			yield return _bot.Through(TerminalStand, sprint: false, arrive: 0.3f);

			Interactable terminal = GameDriver.Get<Interactable>(_driver.Warehouse, "_deliveryPoint");
			Bounds terminalBounds = terminal.GetComponentInChildren<Collider>().bounds;
			Vector3 terminalAim = terminalBounds.center + Vector3.up * (terminalBounds.extents.y * TerminalAimHeightFraction);

			if (_captureCues)
			{
				yield return _bot.Aim(terminalAim);
			}

			yield return CueShot("5_terminal_carrying");
			float end = Time.time + DeliveryTimeoutSeconds;
			bool pressed = false;

			try
			{
				while (!_driver.ResultVisible && Time.time < end)
				{
					_bot.TrackAim(terminalAim);

					bool focused = _player.Interactor.Target == terminal;

					if (focused && (!pressed || terminal.IsHolding))
					{
						_bot.Hold(Key.E);
						pressed = true;
					}
					else
					{
						if (pressed && !focused && terminal.IsAvailable)
						{
							Mark("delivery focus interrupted");
						}

						_bot.Release(Key.E);
						pressed = false;
					}

					yield return _bot.Frame();
				}
			}
			finally
			{
				_bot.Release(Key.E);
			}

			yield return _bot.Frame();

			Assert.That(_driver.ResultVisible, Is.True, $"Holding E at the terminal did not deliver the package (position {_bot.Position:F1}, health {_player.Vitals.Current}, carrying {_player.Carry.IsCarrying}, scanned {_facts.CargoScanned}, target {_player.Interactor.Target}).");

			Mark("result panel open");
		}

		private static ConditionDoor FindDoor(string name)
		{
			foreach (ConditionDoor door in Object.FindObjectsByType<ConditionDoor>(FindObjectsSortMode.None))
			{
				if (door.gameObject.name == name)
				{
					return door;
				}
			}

			Assert.Fail("There is no door named " + name);

			return null;
		}
	}
}
#endif
