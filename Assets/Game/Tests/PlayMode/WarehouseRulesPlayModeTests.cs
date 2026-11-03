#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Packages;
using NightCourier.Player;
using NightCourier.UI;
using NightCourier.World;
using NUnit.Framework;
using RD.Mechanics.PressurePlate;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NightCourier.PlayModeTests
{
	public sealed class WarehouseRulesPlayModeTests
	{
		private const int TestTimeoutMilliseconds = 180000;
		private const string CargoLayerName = "Cargo";
		private const string LightsOut = "contract.lights-out";
		private const string Express = "contract.express";
		private const long ExpressPay = 320;
		private const string RestrictedCargo = "contract.restricted-cargo";
		private const long LightsOutPay = 260;
		private const long RestrictedCargoPay = 450;
		private const long PropertyFine = 20;
		private const long NoViolationsBonus = 100;
		private const int PropertyDamageOnly = 1;
		private const float StrangerObservationSeconds = 4.5f;
		private const float DarkObservationSeconds = 6f;
		private const float StrangerSize = 0.4f;
		private const long StandardDeliveryPay = 150;
		private const string BlockedSaveFolder = "Saves.PlayModeTests.blocked";

		private static readonly Vector3 DockLeverPosition = new(-19.7f, 1.2f, -7f);
		private static readonly Vector3 HallFloor = new(0f, 0.5f, -2f);
		private static readonly Vector3 BarricadeViewpoint = new(-13.5f, 0.1f, -8f);

		private GameDriver _driver;

		[SetUp]
		public void DeleteTestSave()
		{
			GameDriver.DeleteTestSaves();

			_driver = new GameDriver();
		}

		[OneTimeTearDown]
		public void DeleteTestSaveAfterTheRun()
		{
			GameDriver.DeleteTestSaves();
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Scanner_IgnoresStrangers_ScansOnlyThePackage_AndAbortsWhenItLeavesThePlate()
		{
			yield return _driver.StartContract(GameDriver.StandardDelivery);

			WarehouseFacts facts = GameDriver.Get<WarehouseFacts>(_driver.Warehouse, "_facts");
			CargoScanner scanner = Object.FindFirstObjectByType<CargoScanner>();
			PressurePlate plate = GameDriver.Get<PressurePlate>(scanner, "_plate");

			GameDriver.Get<Interactable>(Object.FindFirstObjectByType<PowerLever>(), "_interactable").Press();

			Assert.That(facts.PowerOn, Is.True);

			GameObject stranger = GameObject.CreatePrimitive(PrimitiveType.Cube);

			try
			{
				stranger.layer = LayerMask.NameToLayer(CargoLayerName);
				stranger.transform.localScale = Vector3.one * StrangerSize;
				stranger.transform.position = plate.GetComponent<Collider>().bounds.center;
				stranger.AddComponent<Rigidbody>().isKinematic = true;

				yield return GameDriver.WaitUntil(() => plate.IsActive, "the plate to feel the stranger");
				yield return new WaitForSeconds(StrangerObservationSeconds);

				Assert.That(plate.IsActive, Is.True, "The stranger no longer stands on the plate.");
				Assert.That(scanner.IsScanning, Is.False, "A foreign object started the scan.");
				Assert.That(facts.CargoScanned, Is.False, "A foreign object completed the scan.");

				_driver.PlacePackageOnScanner();

				yield return GameDriver.WaitUntil(() => scanner.IsScanning, "the scan of the real package to start");

				Assert.That(scanner.HasPackageOnPlate, Is.True);

				yield return _driver.PickUpPackage();
				yield return GameDriver.WaitUntil(() => !scanner.IsScanning, "the scan to abort");

				Assert.That(facts.CargoScanned, Is.False, "A package taken off the plate still completed the scan.");
				Assert.That(_driver.NoticeText, Does.Contain("Scan interrupted"));

				_driver.PlacePackageOnScanner();

				yield return GameDriver.WaitUntil(() => facts.CargoScanned, "the scan to complete", GameDriver.ScanTimeoutSeconds);
			}
			finally
			{
				Object.Destroy(stranger);
			}
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator LightsOut_ParkedPackageStaysLit_TheDarkIsAnnounced_AndEveryBlockedObjectExplainsItself()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.AcceptInTerminal(LightsOut);
			yield return _driver.CarryPackageToTheVanAndDepart();

			WarehouseEntry warehouse = _driver.Warehouse;
			WarehouseFacts facts = GameDriver.Get<WarehouseFacts>(warehouse, "_facts");
			WarehouseLighting lighting = GameDriver.Get<WarehouseLighting>(warehouse, "_lighting");
			CargoPackage package = Object.FindFirstObjectByType<CargoPackage>();
			KeepInLightRule rule = package.GetComponent<KeepInLightRule>();
			Vector3 parkedAt = package.transform.position;

			Assert.That(facts.PowerOn, Is.False);
			Assert.That(facts.BatteryRequired, Is.True);
			Assert.That(lighting.IsLit(parkedAt), Is.True, "The work lamp must keep the parked package lit during the blackout.");
			Assert.That(lighting.IsLit(HallFloor), Is.False, "The hall must be dark until the power is restored.");
			Assert.That(lighting.IsLit(DockLeverPosition), Is.False, "The breaker panel must stand outside the safe pool.");

			Assert.That(BlockedKeyOf<ConditionDoor>(warehouse, "_maintenanceDoor"), Is.EqualTo(BlockedReasons.DoorNoPower));
			Assert.That(BlockedKeyOf<ConditionDoor>(warehouse, "_scannerHatch"), Is.EqualTo(BlockedReasons.DoorNoPower));
			Assert.That(BlockedKeyOf<FreightLift>(warehouse, "_freightLift", "_console"), Is.EqualTo(BlockedReasons.LiftNoPower));
			Assert.That(BlockedKeyOf<PowerLever>(warehouse, "_powerLever", "_interactable"), Is.EqualTo(BlockedReasons.BreakerNeedsBattery));
			Assert.That(BlockedKeyOf<GeneratorSocket>(warehouse, "_generator", "_interactable"), Is.EqualTo(BlockedReasons.GeneratorNeedsBattery));
			Assert.That(BlockedKeyOf<BreakableBarricade>(warehouse, "_barricade"), Is.EqualTo(BlockedReasons.BarricadeIntact));

			GameDriver.Teleport(_driver.Player, BarricadeViewpoint);
			_driver.Player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

			yield return GameDriver.WaitUntil(() => _driver.PromptText.Contains("Weak barricade"), "the barricade explanation on the prompt");

			yield return new WaitForSeconds(DarkObservationSeconds / 2f);

			Assert.That(rule.DarkSeconds, Is.EqualTo(0d).Within(0.01d), "The parked package accumulated dark time under the work lamp.");

			GameDriver.MovePackage(package, HallFloor);

			yield return GameDriver.WaitUntil(() => rule.DarkSeconds > 0.5d, "the dark timer to run in the hall");

			Assert.That(_driver.NoticeText, Does.Contain("in the dark"));

			GameDriver.MovePackage(package, parkedAt);

			yield return GameDriver.WaitUntil(() => rule.DarkSeconds <= 0d, "the dark timer to reset under the lamp");

			Assert.That(_driver.NoticeText, Does.Contain("back in the light"));
			Assert.That(_driver.Run.ViolationCount, Is.EqualTo(0), "Passing through the dark briefly must not violate the rule.");
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator LightsOut_FullRun_ParkTheBox_PowerUp_AndDeliverWithoutViolations()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.AcceptInTerminal(LightsOut);
			yield return _driver.CarryPackageToTheVanAndDepart();
			yield return _driver.PowerUpTheBlackout();

			Assert.That(_driver.Run.ViolationCount, Is.EqualTo(PropertyDamageOnly), "Only the barricade fine is expected before the delivery.");

			yield return _driver.ScanAndDeliver();

			Assert.That(_driver.ResultText, Does.Contain("Base pay: " + LightsOutPay));
			Assert.That(_driver.ResultText, Does.Contain("Property damage: -" + PropertyFine));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator RestrictedCargo_ParkedOnTheCrate_PaysTheNoViolationsBonus()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.CompleteInState(GameDriver.Fragile);
			yield return _driver.CompleteInState(LightsOut);
			yield return _driver.CompleteInState(Express);
			yield return _driver.AcceptInTerminal(RestrictedCargo);
			yield return _driver.CarryPackageToTheVanAndDepart();

			foreach (EncounterGroup encounter in GameDriver.Get<EncounterGroup[]>(_driver.Warehouse, "_encounters"))
			{
				encounter.gameObject.SetActive(false);
			}

			yield return _driver.PowerUpTheBlackout();
			yield return _driver.ScanAndDeliver();

			Assert.That(_driver.ResultText, Does.Contain("Base pay: " + RestrictedCargoPay));
			Assert.That(_driver.ResultText, Does.Contain("Property damage: -" + PropertyFine));
			Assert.That(_driver.ResultText, Does.Contain("Rule violations: 0"), "Fetching the battery forced a rule violation.");
			Assert.That(_driver.ResultText, Does.Contain("No violations: " + NoViolationsBonus), "The bonus was lost although only the barricade was broken.");
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null, "The run was not settled.");
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Express_ActivatesBothThreats_AndSettlesWithTheOnTimeBonus()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.CompleteInState(GameDriver.Fragile);
			yield return _driver.CompleteInState(LightsOut);
			yield return _driver.AcceptInTerminal(Express);
			yield return _driver.CarryPackageToTheVanAndDepart();

			EncounterGroup[] encounters = GameDriver.Get<EncounterGroup[]>(_driver.Warehouse, "_encounters");

			foreach (EncounterGroup encounter in encounters)
			{
				bool expected = encounter.Kind == EncounterKind.Crawlers || encounter.Kind == EncounterKind.SecurityDrone;

				Assert.That(encounter.gameObject.activeSelf, Is.EqualTo(expected), encounter.Kind.ToString());
			}

			Assert.That(_driver.Run.HasDeadline, Is.True);

			yield return _driver.ScanAndDeliver();

			Assert.That(_driver.ResultText, Does.Contain("Base pay: " + ExpressPay));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator FailedSave_KeepsTheProgressInMemory_TellsThePlayer_AndPaysOnce()
		{
			string blocker = Path.Combine(Application.persistentDataPath, BlockedSaveFolder);

			File.WriteAllText(blocker, "this file stands where the save folder would be");

			try
			{
				yield return _driver.BootToMainMenu(BlockedSaveFolder + "/inner");
				yield return _driver.NewGameToDepot();
				yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
				yield return _driver.CarryPackageToTheVanAndDepart();

				LogAssert.Expect(LogType.Exception, new Regex("Cannot create .* same name already exists"));

				yield return _driver.ScanAndDeliver();

				Assert.That(_driver.ResultText, Does.Contain("could not be saved"));
				Assert.That(_driver.ResultText, Does.Contain("Base pay: " + StandardDeliveryPay));
				Assert.That(_driver.Campaign.Credits, Is.EqualTo(StandardDeliveryPay), "The payout was lost or doubled when the save failed.");
				Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
				Assert.That(_driver.Services.Campaign.HasSave, Is.False, "A save that never reached the disk was reported as written.");

				yield return _driver.ReturnToDepotFromResult();

				Assert.That(_driver.Campaign.Credits, Is.EqualTo(StandardDeliveryPay), "Leaving the result lost the unsaved payout.");
			}
			finally
			{
				File.Delete(blocker);
			}
		}

		private static string BlockedKeyOf<T>(object owner, string field, string interactableField = "_notice") where T : Component
		{
			T component = GameDriver.Get<T>(owner, field);
			Interactable interactable = GameDriver.Get<Interactable>(component, interactableField);

			return interactable.BlockedKey;
		}
	}
}
#endif
