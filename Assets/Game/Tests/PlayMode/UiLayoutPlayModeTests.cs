#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
using NightCourier.Contracts;
using NightCourier.Packages;
using NightCourier.Player;
using NightCourier.UI;
using NightCourier.World;
using NUnit.Framework;
using RD.ScreenNavigation;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Toolkit = UnityEngine.UIElements;

namespace NightCourier.PlayModeTests
{
	[Category("Visual")]
	public sealed class UiLayoutPlayModeTests
	{
		private const int TestTimeoutMilliseconds = 300000;
		private const string LightsOut = "contract.lights-out";
		private const string Express = "contract.express";
		private const string AuditFolder = "Temp/UiAudit";
		private const bool FailOnProblems = true;

		private static readonly Vector2Int[] Resolutions =
		{
			new(1920, 1080), new(1280, 720), new(2560, 1440), new(1280, 1024), new(1600, 1200), new(2560, 1080),
			new(1280, 800)
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
			yield return WaitForScreen(Object.FindFirstObjectByType<MainMenuEntry>().Navigator, MainMenuEntry.MainScreenId);
			yield return audit.Capture("01_main_menu");

			GameDriver.Click(Object.FindFirstObjectByType<MainMenuEntry>().Root, "entry-" + MainMenuEntry.SettingsEntryId);

			yield return WaitForScreen(Object.FindFirstObjectByType<MainMenuEntry>().Navigator, MainMenuEntry.SettingsScreenId);
			yield return audit.Capture("02_main_settings");

			GameDriver.Click(Object.FindFirstObjectByType<MainMenuEntry>().Root, "settings-back");

			yield return WaitForScreen(Object.FindFirstObjectByType<MainMenuEntry>().Navigator, MainMenuEntry.MainScreenId);
			yield return _driver.NewGameToDepot();
			yield return audit.Capture("03_depot_hud");

			DepotEntry depot = _driver.Depot;

			GameDriver.Invoke(depot, "OpenTerminal");

			yield return null;

			Assert.That(Object.FindAnyObjectByType<PlayerVitalsView>().IsHudHidden, Is.True);
			yield return audit.Capture("04_depot_terminal_one_contract");

			GameDriver.Click(Object.FindFirstObjectByType<ContractTerminalView>().Panel, ContractTerminalView.CloseButtonName);

			UpgradeTerminalView upgrades = Object.FindFirstObjectByType<UpgradeTerminalView>();

			GameDriver.Invoke(upgrades, "Open");

			yield return null;
			yield return audit.Capture("05_upgrade_terminal");

			GameDriver.Click(upgrades.Panel, UpgradeTerminalView.CloseButtonName);

			EquipmentLockerView locker = Object.FindFirstObjectByType<EquipmentLockerView>();

			GameDriver.Invoke(locker, "Open");

			yield return null;
			yield return audit.Capture("06_equipment_locker");

			GameDriver.Click(locker.Panel, EquipmentLockerView.CloseButtonName);

			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.CompleteInState(GameDriver.Fragile);

			GameDriver.Invoke(depot, "OpenTerminal");

			yield return null;
			yield return audit.Capture("07_depot_terminal_two_contracts");

			GameDriver.Click(Object.FindFirstObjectByType<ContractTerminalView>().Panel, ContractTerminalView.CloseButtonName);

			yield return _driver.AcceptInTerminal(LightsOut);
			yield return null;
			yield return audit.Capture("08_depot_contract_accepted");

			yield return _driver.CarryPackageToTheVanAndDepart();
			yield return new WaitForSeconds(0.5f);
			yield return audit.Capture("09_warehouse_start_blackout");

			Vector3 dockPosition = _driver.Player.transform.position;
			Quaternion dockRotation = _driver.Player.transform.rotation;

			GameDriver.Teleport(_driver.Player, new Vector3(-13.5f, 0.1f, -8f));
			_driver.Player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

			yield return new WaitForSeconds(0.5f);
			yield return audit.Capture("10_warehouse_barricade_prompt");

			CargoPackage package = Object.FindFirstObjectByType<CargoPackage>();

			GameDriver.MovePackage(package, new Vector3(0f, 0.5f, -2f));

			yield return new WaitForSeconds(1.5f);
			Assert.That(Object.FindAnyObjectByType<PackageHudView>().DarkTimerVisible, Is.True);
			yield return audit.Capture("11_warehouse_dark_notice");

			GameDriver.MovePackage(package, new Vector3(-15.6f, 1.2f, -11.9f));

			PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>();

			pause.Open();

			yield return WaitForScreen(pause.Navigator, PauseMenu.MainScreenId);

			Assert.That(Object.FindAnyObjectByType<PlayerVitalsView>().IsHudHidden, Is.True);
			yield return audit.Capture("12_pause_menu");

			GameDriver.Click(pause.Root, "entry-" + PauseMenu.SettingsEntryId);

			yield return WaitForScreen(pause.Navigator, PauseMenu.SettingsScreenId);
			AssertVisible(Toolkit.UQueryExtensions.Q<Toolkit.Label>(pause.SettingsView.Root, "settings-title"));
			yield return audit.Capture("13_pause_settings");

			GameDriver.Click(pause.Root, "settings-back");

			yield return WaitForScreen(pause.Navigator, PauseMenu.MainScreenId);

			GameDriver.Click(pause.Root, "entry-" + PauseMenu.ControlsEntryId);

			yield return WaitForScreen(pause.Navigator, PauseMenu.ControlsScreenId);
			yield return audit.Capture("14_pause_controls");

			GameDriver.Click(pause.Root, "controls-back");

			yield return WaitForScreen(pause.Navigator, PauseMenu.MainScreenId);

			pause.Close();

			GameDriver.Teleport(_driver.Player, dockPosition);
			_driver.Player.transform.rotation = dockRotation;

			yield return _driver.PowerUpTheBlackout();
			yield return _driver.ScanAndDeliver();
			yield return null;

			Assert.That(Object.FindAnyObjectByType<PlayerVitalsView>().IsHudHidden, Is.True);
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
			Assert.That(Object.FindAnyObjectByType<PlayerVitalsView>().OverlayAlpha, Is.EqualTo(0f));
			yield return audit.Capture("18_result_failed");

			GameDriver.Get<ResultPanelView>(_driver.Warehouse, "_resultView").ShowFailure(
				_driver.Services.Localization,
				GameDriver.Get<ContractRun>(_driver.Warehouse, "_run").Spec,
				"result.settlement_failed",
				true);
			yield return audit.Capture("19_payment_pending");

			audit.Save();

			if (FailOnProblems)
			{
				Assert.That(audit.Problems, Is.Zero, audit.Report);
			}
		}

		private static IEnumerator WaitForScreen(IScreenNavigator navigator, string screenId)
		{
			yield return GameDriver.WaitUntil(
				() => !navigator.IsTransitioning && navigator.CurrentScreenId == screenId,
				"the completed transition to " + screenId);

			yield return null;
		}

		private static void AssertVisible(Toolkit.VisualElement element)
		{
			Assert.That(element, Is.Not.Null, "The expected screen element is missing.");

			for (Toolkit.VisualElement current = element; current != null; current = current.parent)
			{
				Assert.That(current.resolvedStyle.display, Is.Not.EqualTo(Toolkit.DisplayStyle.None), current.name);
				Assert.That(current.resolvedStyle.visibility, Is.EqualTo(Toolkit.Visibility.Visible), current.name);
				Assert.That(current.resolvedStyle.opacity, Is.GreaterThan(0.95f), current.name);
			}

			Assert.That(element.worldBound.width, Is.GreaterThan(0f));
			Assert.That(element.worldBound.height, Is.GreaterThan(0f));
		}
	}
}
#endif