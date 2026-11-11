#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Text.RegularExpressions;
using RD.SceneFlow;
using System.IO;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.UI;
using RD.DependencyInjection;
using RD.Mechanics.Interactable;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NightCourier.PlayModeTests
{
	public sealed class GameLoopPlayModeTests
	{
		private const int TestTimeoutMilliseconds = 180000;
		private const string InjectedUnloadFailure = "Injected unload failure.";
		private const string WarehouseSceneName = "Warehouse";
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
		public IEnumerator FailedContentLoads_KeepTheCurrentScene_AndAllowRetry()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			yield return _driver.PickUpPackage();

			DepotEntry depot = _driver.Depot;
			ISceneFlow flow = GameDriver.Get<ISceneFlow>(_driver.Host, "_sceneFlow");
			bool injected = false;

			void FailOnce(float progress)
			{
				if (!injected)
				{
					injected = true;
					throw new InvalidOperationException("Injected content load failure.");
				}
			}

			flow.ProgressChanged += FailOnce;

			try
			{
				LogAssert.Expect(LogType.Exception, new Regex("Injected content load failure"));
				GameDriver.Get<Interactable>(depot, "_van").Press();

				yield return GameDriver.WaitUntil(
					() => injected && !GameDriver.Get<bool>(_driver.Host, "_isNavigating") && _driver.Run == null,
					"the failed departure to recover");

				Assert.That(_driver.Depot, Is.SameAs(depot));
				Assert.That(_driver.Services.Input.Actions.Gameplay.enabled, Is.True);
				Assert.That(_driver.Services.Input.Actions.Ui.enabled, Is.False);
				Assert.That(flow.IsLoaded(SceneIds.Depot), Is.True);
				Assert.That(flow.IsLoaded(SceneIds.Warehouse), Is.False);
				Assert.That(_driver.Campaign.Credits, Is.Zero);
				Assert.That(_driver.Campaign.Contracts.GetAvailable()[0].Id, Is.EqualTo(GameDriver.StandardDelivery));
			}
			finally
			{
				flow.ProgressChanged -= FailOnce;
			}

			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			yield return _driver.CarryPackageToTheVanAndDepart();
			yield return _driver.ScanAndDeliver();

			Assert.That(_driver.ResultText, Does.Contain("Base pay: " + StandardDeliveryPay));

			ResultPanelView result = GameDriver.Get<ResultPanelView>(_driver.Warehouse, "_resultView");
			long credits = _driver.Campaign.Credits;
			injected = false;
			flow.ProgressChanged += FailOnce;

			try
			{
				LogAssert.Expect(LogType.Exception, new Regex("Injected content load failure"));
				GameDriver.Click(result.Panel, ResultPanelView.ReturnButtonName);

				yield return GameDriver.WaitUntil(
					() => injected && !GameDriver.Get<bool>(_driver.Host, "_isNavigating") && result.ReturnEnabled,
					"the failed return to recover");

				Assert.That(_driver.ResultVisible, Is.True);
				Assert.That(flow.IsLoaded(SceneIds.Warehouse), Is.True);
				Assert.That(flow.IsLoaded(SceneIds.Depot), Is.False);
				Assert.That(_driver.Services.Input.Actions.Ui.enabled, Is.True);
				Assert.That(_driver.Campaign.Credits, Is.EqualTo(credits));
			}
			finally
			{
				flow.ProgressChanged -= FailOnce;
			}

			yield return _driver.ReturnToDepotFromResult();
			yield return _driver.ReturnToMainMenu();
			yield return _driver.ContinueToDepot();

			Assert.That(_driver.Campaign.Credits, Is.EqualTo(credits));
			Assert.That(_driver.Campaign.Contracts.CompletedCount, Is.EqualTo(1));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator UnloadFailingBeforeItStarts_KeepsTheDepot_AndAllowsRetry()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			yield return _driver.PickUpPackage();

			DepotEntry depot = _driver.Depot;
			ISceneFlow flow = GameDriver.Get<ISceneFlow>(_driver.Host, "_sceneFlow");
			bool injected = false;
			Action<float> failure = FailAfterWarehouseLoads(flow, 1, () => injected = true);

			flow.ProgressChanged += failure;

			try
			{
				LogAssert.Expect(LogType.Exception, new Regex(InjectedUnloadFailure));
				GameDriver.Get<Interactable>(depot, "_van").Press();

				yield return GameDriver.WaitUntil(
					() => injected && !_driver.Host.IsNavigating && _driver.Run == null,
					"the failed unload to recover");
			}
			finally
			{
				flow.ProgressChanged -= failure;
			}

			Assert.That(_driver.Depot, Is.SameAs(depot));
			Assert.That(GameDriver.Get<string>(_driver.Host, "_currentSceneId"), Is.EqualTo(SceneIds.Depot));
			Assert.That(flow.IsLoaded(SceneIds.Depot), Is.True);
			Assert.That(flow.IsLoaded(SceneIds.Warehouse), Is.False);
			Assert.That(SceneManager.GetActiveScene().path, Does.EndWith("Depot.unity"));
			Assert.That(_driver.Services.Input.Actions.Gameplay.enabled, Is.True);
			Assert.That(_driver.Campaign.Credits, Is.Zero);

			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			yield return _driver.CarryPackageToTheVanAndDepart();

			Assert.That(flow.IsLoaded(SceneIds.Depot), Is.False);
			Assert.That(GameDriver.Get<string>(_driver.Host, "_currentSceneId"), Is.EqualTo(SceneIds.Warehouse));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator UnloadFailingAfterItStarted_ContinuesToTheDestination()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			yield return _driver.PickUpPackage();

			ISceneFlow flow = GameDriver.Get<ISceneFlow>(_driver.Host, "_sceneFlow");
			bool injected = false;
			Action<float> failure = FailAfterWarehouseLoads(flow, 2, () => injected = true);

			flow.ProgressChanged += failure;

			try
			{
				LogAssert.Expect(LogType.Exception, new Regex(InjectedUnloadFailure));
				GameDriver.Get<Interactable>(_driver.Depot, "_van").Press();

				yield return GameDriver.WaitUntil(
					() => injected && !_driver.Host.IsNavigating && GameDriver.IsEntered(_driver.Warehouse),
					"the warehouse after the unload error");
			}
			finally
			{
				flow.ProgressChanged -= failure;
			}

			Assert.That(flow.IsLoaded(SceneIds.Depot), Is.False);
			Assert.That(GameDriver.Get<string>(_driver.Host, "_currentSceneId"), Is.EqualTo(SceneIds.Warehouse));
			Assert.That(SceneManager.GetActiveScene().path, Does.EndWith("Warehouse.unity"));
			Assert.That(_driver.Run, Is.Not.Null);
			Assert.That(_driver.Run.Phase, Is.EqualTo(ContractPhase.InTransit));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator SceneEntryFailing_RestoresTheMainMenu_AndContinueReturnsToTheDepot()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			yield return _driver.PickUpPackage();
			yield return _driver.SaveCampaign();

			ISceneFlow flow = GameDriver.Get<ISceneFlow>(_driver.Host, "_sceneFlow");
			bool injected = false;

			void AddSecondEntry(float progress)
			{
				if (injected || !flow.IsLoaded(SceneIds.Warehouse))
				{
					return;
				}

				injected = true;

				GameObject second = new("PH_SecondSceneEntry");

				SceneManager.MoveGameObjectToScene(second, SceneManager.GetSceneByName(WarehouseSceneName));
				second.AddComponent<SecondSceneEntry>();
			}

			flow.ProgressChanged += AddSecondEntry;

			try
			{
				LogAssert.Expect(LogType.Exception, new Regex("exactly one scene entry"));
				GameDriver.Get<Interactable>(_driver.Depot, "_van").Press();

				yield return GameDriver.WaitUntil(
					() => injected
						&& !_driver.Host.IsNavigating
						&& GameDriver.IsEntered(Object.FindFirstObjectByType<MainMenuEntry>()),
					"the main menu after the failed scene entry");
			}
			finally
			{
				flow.ProgressChanged -= AddSecondEntry;
			}

			Assert.That(GameDriver.Get<string>(_driver.Host, "_currentSceneId"), Is.EqualTo(SceneIds.MainMenu));
			Assert.That(flow.IsLoaded(SceneIds.Warehouse), Is.False);
			Assert.That(flow.IsLoaded(SceneIds.Depot), Is.False);
			Assert.That(Object.FindObjectsByType<SecondSceneEntry>(FindObjectsInactive.Include), Is.Empty);

			yield return _driver.ContinueToDepot();

			Assert.That(_driver.Run, Is.Null);
			Assert.That(_driver.Campaign.Contracts.GetAvailable()[0].Id, Is.EqualTo(GameDriver.StandardDelivery));
		}

		private static Action<float> FailAfterWarehouseLoads(ISceneFlow flow, int callbackNumber, Action onFailure)
		{
			bool warehouseLoaded = false;
			bool failed = false;
			int callbacks = 0;

			return _ =>
			{
				if (failed)
				{
					return;
				}

				if (!warehouseLoaded)
				{
					warehouseLoaded = flow.IsLoaded(SceneIds.Warehouse);

					return;
				}

				callbacks++;

				if (callbacks == callbackNumber)
				{
					failed = true;
					onFailure();

					throw new InvalidOperationException(InjectedUnloadFailure);
				}
			};
		}

		public sealed class SecondSceneEntry : MonoBehaviour, ISceneEntry
		{
			public void Enter(GameServices services, IObjectResolver scene)
			{
			}
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
				ContractTerminalView panel = Object.FindFirstObjectByType<ContractTerminalView>();
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
				yield return GameDriver.WaitUntil(() => panel.IsOpen, "keyboard interaction opening the terminal");

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
				yield return GameDriver.WaitUntil(() => !panel.IsOpen, "keyboard cancel closing the terminal");

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
			Assert.That(pause.IsLeaveAvailable, Is.False);

			yield return _driver.AcceptInTerminal(GameDriver.StandardDelivery);
			Assert.That(GameDriver.Get<FirstDeliveryTraining>(_driver.Depot, "_training").StepNumber, Is.EqualTo(2));
			yield return _driver.PickUpPackage();
			Assert.That(GameDriver.Get<FirstDeliveryTraining>(_driver.Depot, "_training").StepNumber, Is.EqualTo(3));
			Assert.That(_driver.Player.Carry.Drop(), Is.True);
			GameDriver.MovePackage(Object.FindFirstObjectByType<NightCourier.Player.CargoPackage>(), new Vector3(0f, -100f, 0f));
			DepotEntry previous = _driver.Depot;
			pause.Open();
			Assert.That(pause.IsLeaveAvailable, Is.True);
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
