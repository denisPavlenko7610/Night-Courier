#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
using NightCourier.Contracts;
using NightCourier.Packages;
using NightCourier.Player;
using NightCourier.UI;
using NightCourier.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NightCourier.PlayModeTests
{
	public sealed class UiLayoutPlayModeTests
	{
		private const int TestTimeoutMilliseconds = 300000;
		private const string LightsOut = "contract.lights-out";
		private const string Express = "contract.express";
		private const string AuditFolder = "Temp/UiAudit";
		private const bool FailOnProblems = true;
		private const float PauseFadeSeconds = 0.8f;

		private static readonly Vector2Int[] Resolutions =
		{
			new(1920, 1080), new(1280, 720), new(2560, 1440), new(1280, 1024), new(1600, 1200), new(2560, 1080), new(1280, 800)
		};

		private GameDriver _driver;

		[SetUp]
		public void DeleteTestSave()
		{
			GameDriver.DeleteTestSaves();

			_driver = new GameDriver();
		}

		[OneTimeTearDown]
		public void Cleanup()
		{
			GameDriver.DeleteTestSaves();
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Screens_HaveNoOverlappingOrClippedText([ValueSource(nameof(Resolutions))] Vector2Int size)
		{
			int width = size.x;
			int height = size.y;

			UiAudit.SetGameViewSize(width, height);

			yield return null;
			yield return null;

			UiAudit audit = new(Path.Combine(AuditFolder, width + "x" + height));

			yield return _driver.BootToMainMenu();
			yield return new WaitForSeconds(1.2f);
			yield return audit.Capture("01_main_menu");

			GameDriver.Get<Button>(Object.FindFirstObjectByType<MainMenuEntry>(), "_settingsButton").onClick.Invoke();

			yield return null;
			yield return audit.Capture("02_main_settings");

			GameDriver.Get<Button>(Object.FindFirstObjectByType<MainMenuEntry>(), "_settingsBackButton").onClick.Invoke();

			yield return _driver.NewGameToDepot();
			yield return audit.Capture("03_depot_hud");

			DepotEntry depot = _driver.Depot;

			GameDriver.Invoke(depot, "OpenTerminal");

			yield return null;
			yield return audit.Capture("04_depot_terminal_one_contract");

			GameDriver.Get<Button>(depot, "_closeButton").onClick.Invoke();

			UpgradeTerminalView upgrades = Object.FindFirstObjectByType<UpgradeTerminalView>();

			GameDriver.Invoke(upgrades, "Open");

			yield return null;
			yield return audit.Capture("05_upgrade_terminal");

			GameDriver.Get<Button>(upgrades, "_closeButton").onClick.Invoke();

			EquipmentLockerView locker = Object.FindFirstObjectByType<EquipmentLockerView>();

			GameDriver.Invoke(locker, "Open");

			yield return null;
			yield return audit.Capture("06_equipment_locker");

			GameDriver.Get<Button>(locker, "_closeButton").onClick.Invoke();

			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.CompleteInState(GameDriver.Fragile);

			GameDriver.Invoke(depot, "OpenTerminal");

			yield return null;
			yield return audit.Capture("07_depot_terminal_two_contracts");

			GameDriver.Get<Button>(depot, "_closeButton").onClick.Invoke();

			yield return _driver.AcceptInTerminal(LightsOut);
			yield return null;
			yield return audit.Capture("08_depot_contract_accepted");

			yield return _driver.CarryPackageToTheVanAndDepart();
			yield return new WaitForSeconds(0.5f);
			yield return audit.Capture("09_warehouse_start_blackout");

			GameDriver.Teleport(_driver.Player, new Vector3(-13.5f, 0.1f, -8f));
			_driver.Player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

			yield return new WaitForSeconds(0.5f);
			yield return audit.Capture("10_warehouse_barricade_prompt");

			CargoPackage package = Object.FindFirstObjectByType<CargoPackage>();

			GameDriver.MovePackage(package, new Vector3(0f, 0.5f, -2f));

			yield return new WaitForSeconds(1.5f);
			yield return audit.Capture("11_warehouse_dark_notice");

			GameDriver.MovePackage(package, new Vector3(-15.6f, 1.2f, -11.9f));

			PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>();

			pause.Open();

			yield return new WaitForSecondsRealtime(PauseFadeSeconds);
			yield return audit.Capture("12_pause_menu");

			GameDriver.Get<Button>(pause, "_settingsButton").onClick.Invoke();

			yield return new WaitForSecondsRealtime(PauseFadeSeconds);
			yield return audit.Capture("13_pause_settings");

			GameDriver.Get<Button>(pause, "_settingsBackButton").onClick.Invoke();
			GameDriver.Get<Button>(pause, "_controlsButton").onClick.Invoke();

			yield return new WaitForSecondsRealtime(PauseFadeSeconds);
			yield return audit.Capture("14_pause_controls");

			GameDriver.Get<Button>(pause, "_controlsBackButton").onClick.Invoke();
			pause.Close();

			yield return _driver.PowerUpTheBlackout();
			yield return _driver.ScanAndDeliver();
			yield return null;
			yield return audit.Capture("15_result_success");

			yield return _driver.ReturnToDepotFromResult();
			yield return audit.Capture("16_depot_after_result");

			yield return _driver.AcceptInTerminal(Express);
			yield return _driver.CarryPackageToTheVanAndDepart();

			_driver.KillPlayer();

			yield return new WaitForSeconds(0.8f);
			yield return audit.Capture("17_death_overlay");
			yield return GameDriver.WaitUntil(() => _driver.Player.Vitals.IsAlive, "the courier recovery respawn", 15f);

			_driver.DamagePackage(1000d);

			yield return GameDriver.WaitUntil(() => _driver.ResultVisible, "the failure panel");
			yield return null;
			yield return audit.Capture("18_result_failed");

			audit.Save();

			if (FailOnProblems)
			{
				Assert.That(audit.Problems, Is.Zero, audit.Report);
			}
		}
	}
}
#endif
