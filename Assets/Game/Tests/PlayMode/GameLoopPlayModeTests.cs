#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NightCourier.PlayModeTests
{
	public sealed class GameLoopPlayModeTests
	{
		private const int TestTimeoutMilliseconds = 180000;
		private const long StandardDeliveryPay = 150;
		private const long FragilePayWithBonus = 320;
		private const long DeathFee = 25;
		private const long StandardDeliveryExperience = 120;

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

		private IEnumerator NewGameAndStart(string contractId)
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.AcceptInTerminal(contractId);
			yield return _driver.CarryPackageToTheVanAndDepart();
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator InvalidPackagePrefab_DoesNotAcceptTheContract_AndRetrySucceeds()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();

			DepotEntry depot = _driver.Depot;
			var field = typeof(DepotEntry).GetField("_packagePrefab",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			GameObject original = (GameObject)field.GetValue(depot);
			GameObject invalid = new("InvalidPackagePrefab");

			try
			{
				field.SetValue(depot, invalid);
				GameDriver.Invoke(depot, "OnSelected", _driver.Campaign.Contracts.GetAvailable()[0]);
				LogAssert.Expect(LogType.Exception,
					new System.Text.RegularExpressions.Regex("The depot package prefab requires CargoPackage"));
				GameDriver.Invoke(depot, "OnAccept");

				Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
				Assert.That(_driver.Campaign.Contracts.GetAvailable().Count, Is.EqualTo(1));
			}
			finally
			{
				field.SetValue(depot, original);
				Object.Destroy(invalid);
			}

			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			Assert.That(_driver.Run.Phase, Is.EqualTo(ContractPhase.Accepted));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator FullLoop_PaysOnce_SurvivesDoubleInput_AndContinueRestoresTheProfile()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();

			Assert.That(GameDriver.Get<FirstDeliveryTraining>(_driver.Depot, "_training").StepNumber, Is.EqualTo(1));
			Assert.That(_driver.Campaign.Credits, Is.EqualTo(0));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);

			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			Assert.That(_driver.Run.Phase, Is.EqualTo(ContractPhase.Accepted));

			yield return _driver.CarryPackageToTheVanAndDepart();

			Assert.That(_driver.Run.Phase, Is.EqualTo(ContractPhase.InTransit));
			Assert.That(GameDriver.Get<FirstDeliveryTraining>(_driver.Warehouse, "_training").StepNumber, Is.EqualTo(1));

			yield return _driver.ScanAndDeliver();

			Assert.That(_driver.ResultText, Does.Contain("Base pay: " + StandardDeliveryPay));
			Assert.That(_driver.Campaign.Credits, Is.EqualTo(StandardDeliveryPay), "The double delivery press paid twice or not at all.");
			Assert.That(_driver.Campaign.Rank.TotalExperience, Is.EqualTo(StandardDeliveryExperience));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
			Assert.That(File.Exists(Path.Combine(GameDriver.SaveRoot, CampaignRepository.SlotId + ".json")), Is.True, "The result was not saved.");

			yield return _driver.ReturnToDepotFromResult();

			Assert.That(GameDriver.Get<FirstDeliveryTraining>(_driver.Depot, "_training"), Is.Null);

			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
			Assert.That(_driver.Campaign.Credits, Is.EqualTo(StandardDeliveryPay));

			yield return _driver.ReturnToMainMenu();
			yield return _driver.ContinueToDepot();

			Assert.That(_driver.Campaign.Credits, Is.EqualTo(StandardDeliveryPay));
			Assert.That(_driver.Campaign.Rank.TotalExperience, Is.EqualTo(StandardDeliveryExperience));
			Assert.That(_driver.Campaign.Contracts.GetAvailable().Count, Is.GreaterThan(0));

			foreach (ContractSpec spec in _driver.Campaign.Contracts.GetAvailable())
			{
				Assert.That(spec.Id, Is.Not.EqualTo(GameDriver.StandardDelivery), "A completed contract came back after Continue.");
			}
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator DestroyedPackage_FailsWithoutPayout_AndTheRetryPaysOnce()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);

			long creditsBefore = _driver.Campaign.Credits;

			yield return _driver.AcceptInTerminal(GameDriver.Fragile);
			yield return _driver.CarryPackageToTheVanAndDepart();

			_driver.DamagePackage(1000d);

			yield return GameDriver.WaitUntil(() => _driver.ResultVisible, "the failure panel");

			Assert.That(_driver.ResultText, Does.Contain("destroyed"));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
			Assert.That(_driver.Campaign.Credits, Is.EqualTo(creditsBefore), "A failed contract paid out.");

			yield return _driver.ReturnToDepotFromResult();
			yield return _driver.AcceptInTerminal(GameDriver.Fragile);
			yield return _driver.CarryPackageToTheVanAndDepart();
			yield return _driver.ScanAndDeliver();

			Assert.That(_driver.Campaign.Credits, Is.EqualTo(creditsBefore + FragilePayWithBonus), "The retry did not pay exactly once.");
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator AbortFromThePauseMenu_ReturnsToTheDepotAndKeepsTheContractAvailable()
		{
			yield return NewGameAndStart(GameDriver.StandardDelivery);

			PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>();

			pause.Open();

			Assert.That(pause.IsOpen, Is.True);
			Assert.That(Time.timeScale, Is.EqualTo(0f));

			GameDriver.Invoke(pause, "ConfirmLeave");

			yield return _driver.WaitForDepot("the depot after the abort");

			Assert.That(Time.timeScale, Is.EqualTo(1f), "The abort left the game paused.");
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
			Assert.That(_driver.Campaign.Credits, Is.EqualTo(0));
			Assert.That(_driver.Campaign.Contracts.GetAvailable().Count, Is.EqualTo(1));
			Assert.That(_driver.Campaign.Contracts.GetAvailable()[0].Id, Is.EqualTo(GameDriver.StandardDelivery));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator LostDepotPackage_CanBeAborted_AndTrainingRestartsForRetry()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>();
			Assert.That(GameDriver.Get<UnityEngine.UI.Button>(pause, "_leaveButton").gameObject.activeSelf, Is.False);

			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			Assert.That(GameDriver.Get<FirstDeliveryTraining>(_driver.Depot, "_training").StepNumber, Is.EqualTo(2));
			yield return _driver.PickUpPackage();
			Assert.That(GameDriver.Get<FirstDeliveryTraining>(_driver.Depot, "_training").StepNumber, Is.EqualTo(3));
			Assert.That(_driver.Player.Carry.Drop(), Is.True);
			GameDriver.MovePackage(Object.FindFirstObjectByType<NightCourier.Player.CargoPackage>(), new Vector3(0f, -100f, 0f));
			DepotEntry previous = _driver.Depot;
			pause.Open();
			Assert.That(GameDriver.Get<UnityEngine.UI.Button>(pause, "_leaveButton").gameObject.activeSelf, Is.True);
			GameDriver.Invoke(pause, "ConfirmLeave");
			yield return GameDriver.WaitUntil(() => previous == null && _driver.Depot != null, "a fresh depot after the abort");
			yield return _driver.WaitForDepot("the initialized depot after the abort");
			Assert.That(Time.timeScale, Is.EqualTo(1f));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
			Assert.That(_driver.Campaign.Credits, Is.Zero);
			Assert.That(GameDriver.Get<FirstDeliveryTraining>(_driver.Depot, "_training").StepNumber, Is.EqualTo(1));
			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			yield return _driver.CarryPackageToTheVanAndDepart();
			yield return _driver.ScanAndDeliver();
			Assert.That(_driver.Campaign.Credits, Is.EqualTo(StandardDeliveryPay));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator DeathInTransit_RespawnsAndTheRecoveryFeeIsCharged()
		{
			yield return NewGameAndStart(GameDriver.StandardDelivery);

			_driver.KillPlayer();

			yield return GameDriver.WaitUntil(() => _driver.Run.DeathCount == 1, "the death to be counted");
			yield return GameDriver.WaitUntil(() => _driver.Player.Vitals.IsAlive, "the courier recovery respawn", 15f);

			yield return _driver.ScanAndDeliver();

			Assert.That(_driver.ResultText, Does.Contain("Recovery team fee: -" + DeathFee));
			Assert.That(_driver.Campaign.Credits, Is.EqualTo(StandardDeliveryPay - DeathFee));
			Assert.That(_driver.Campaign.Contracts.ActiveRun, Is.Null);
		}
	}
}
#endif
