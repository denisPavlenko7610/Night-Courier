#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.UI;
using RD.Mechanics.Interactable;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
		private const float InputObservationSeconds = 0.2f;
		private const float TerminalApproachDistance = 1.8f;
		private const float MinimumInputMovement = 0.1f;

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
		public IEnumerator KeyboardInput_MovesThePlayer_OpensTheFocusedTerminal_AndCancelRestoresGameplay()
		{
			var background = InputSystem.settings.backgroundBehavior;
			var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
			Keyboard keyboard = null;

			try
			{
				InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
				InputSystem.settings.editorInputBehaviorInPlayMode =
					InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
				yield return _driver.BootToMainMenu();
				yield return _driver.NewGameToDepot();

				keyboard = InputSystem.AddDevice<Keyboard>();
				Vector3 initialPosition = _driver.Player.transform.position;
				InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
				yield return new WaitForSeconds(InputObservationSeconds);

				InputSystem.QueueStateEvent(keyboard, new KeyboardState());
				yield return null;

				Assert.That(
					Vector3.Dot(_driver.Player.transform.position - initialPosition, _driver.Player.transform.forward),
					Is.GreaterThan(MinimumInputMovement)
				);

				Interactable terminal = GameDriver.Get<Interactable>(_driver.Depot, "_terminal");
				GameObject panel = GameDriver.Get<GameObject>(_driver.Depot, "_terminalPanel");
				Vector3 target = terminal.GetComponent<Collider>().bounds.center;
				Vector3 approach = target - Vector3.forward * TerminalApproachDistance;
				approach.y = _driver.Player.transform.position.y;
				GameDriver.Teleport(_driver.Player, approach);
				_driver.Player.transform.rotation = Quaternion.identity;
				Transform origin = GameDriver.Get<Transform>(_driver.Player.Interactor, "_origin");
				origin.rotation = Quaternion.LookRotation(target - origin.position);
				Physics.SyncTransforms();
				yield return null;
				yield return null;

				Assert.That(_driver.Player.Interactor.Target, Is.EqualTo(terminal));

				InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
				yield return GameDriver.WaitUntil(() => panel.activeSelf, "keyboard interaction opening the terminal");

				Assert.That(_driver.Services.Input.Actions.Gameplay.enabled, Is.False);
				Assert.That(_driver.Services.Input.Actions.Ui.enabled, Is.True);
				Vector3 menuPosition = _driver.Player.transform.position;
				InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
				yield return new WaitForSeconds(InputObservationSeconds);
				Assert.That(Vector3.Distance(menuPosition, _driver.Player.transform.position), Is.LessThan(MinimumInputMovement));
				Assert.That(_driver.Player.Interactor.enabled, Is.False);
				Assert.That(_driver.Player.Interactor.Target, Is.Null);
				InputSystem.QueueStateEvent(keyboard, new KeyboardState());
				yield return null;

				InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
				yield return GameDriver.WaitUntil(() => !panel.activeSelf, "keyboard cancel closing the terminal");

				Assert.That(_driver.Services.Input.Actions.Gameplay.enabled, Is.True);
				InputSystem.QueueStateEvent(keyboard, new KeyboardState());
				yield return new WaitForSeconds(InputObservationSeconds);
				Assert.That(_driver.Services.Input.Actions.Ui.enabled, Is.False);
				Assert.That(_driver.Services.Input.Actions.Gameplay.enabled, Is.True);
				Assert.That(Time.timeScale, Is.EqualTo(1f));
			}
			finally
			{
				if (keyboard != null)
				{
					InputSystem.RemoveDevice(keyboard);
				}
				InputSystem.settings.backgroundBehavior = background;
				InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
			}
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
