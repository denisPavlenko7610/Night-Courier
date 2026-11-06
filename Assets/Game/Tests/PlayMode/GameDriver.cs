#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Player;
using NightCourier.UI;
using NightCourier.World;
using NUnit.Framework;
using RD.Health;
using RD.Mechanics.Combat;
using RD.Mechanics.Interactable;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Toolkit = UnityEngine.UIElements;

namespace NightCourier.PlayModeTests
{
	internal sealed class GameDriver
	{
		public const string StandardDelivery = "contract.standard-delivery";
		public const string Fragile = "contract.fragile";
		public const float ScanTimeoutSeconds = 15f;

		private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
		private const string BootScene = "Boot";
		private const float DefaultTimeoutSeconds = 20f;
		private const float PickUpDistance = 1f;
		private const float HoldStepSeconds = 0.02f;
		private const int MaxHoldSteps = 400;
		private const float ScannerPackageLift = 0.45f;
		private const float ScannerApproach = 2.5f;
		private const float DockPathRadius = 0.35f;
		private const float DockPathLowerHeight = 0.45f;
		private const float DockPathUpperHeight = 1.5f;
		private static readonly Vector3 DockAisleStart = new(-18f, 0f, -13f);
		private static readonly Vector3 DockAisleEnd = new(-18f, 0f, -6.5f);

		public const string TestSaveFolder = "Saves.PlayModeTests";

		public static string SaveRoot => Path.Combine(Application.persistentDataPath, TestSaveFolder);

		public GameHost Host { get; private set; }

		public GameServices Services => Get<GameServices>(Host, "_services");

		public CampaignState Campaign => Services.Campaign.Current;

		public PlayerSession Player => Object.FindFirstObjectByType<PlayerSession>();

		public WarehouseEntry Warehouse => Object.FindFirstObjectByType<WarehouseEntry>();

		public DepotEntry Depot => Object.FindFirstObjectByType<DepotEntry>();

		public ContractRun Run => Campaign.Contracts.ActiveRun;

		public static T Get<T>(object owner, string field)
		{
			FieldInfo info = owner.GetType().GetField(field, Private)
				?? throw new MissingFieldException(owner.GetType().Name, field);

			return (T)info.GetValue(owner);
		}

		public static void Set(object owner, string field, object value)
		{
			FieldInfo info = owner.GetType().GetField(field, Private)
				?? throw new MissingFieldException(owner.GetType().Name, field);

			info.SetValue(owner, value);
		}

		public static void DeleteTestSaves()
		{
			string root = Path.GetFullPath(SaveRoot);
			string real = Path.GetFullPath(Path.Combine(Application.persistentDataPath, CampaignStep.SaveFolder));

			if (string.Equals(root, real, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("The test save folder must never be the real save folder.");
			}

			if (Directory.Exists(root))
			{
				Directory.Delete(root, true);
			}
		}

		public static void Invoke(object owner, string method, params object[] arguments)
		{
			MethodInfo info = owner.GetType().GetMethod(method, Private)
				?? throw new MissingMethodException(owner.GetType().Name, method);

			info.Invoke(owner, arguments);
		}

		public static IEnumerator WaitUntil(Func<bool> condition, string description, float timeoutSeconds = DefaultTimeoutSeconds)
		{
			float deadline = Time.realtimeSinceStartup + timeoutSeconds;

			while (!condition())
			{
				if (Time.realtimeSinceStartup > deadline)
				{
					Assert.Fail($"Timed out after {timeoutSeconds}s waiting for {description}.");
				}

				yield return null;
			}
		}

		public static void Teleport(PlayerSession player, Vector3 position)
		{
			CharacterController controller = player.GetComponent<CharacterController>();

			controller.enabled = false;
			player.transform.position = position;
			controller.enabled = true;
		}

		public static void PressAndHold(Interactable interactable)
		{
			interactable.Press();

			for (int step = 0; step < MaxHoldSteps && interactable.IsAvailable && interactable.RequiresHold; step++)
			{
				interactable.Hold(HoldStepSeconds);
			}
		}

		public IEnumerator BootToMainMenu(string saveFolder = TestSaveFolder)
		{
			void RedirectSaves(Scene scene, LoadSceneMode mode)
			{
				GameHost host = Object.FindFirstObjectByType<GameHost>();

				if (host != null)
				{
					Set(host, "_saveFolder", saveFolder);
				}
			}

			SceneManager.sceneLoaded += RedirectSaves;

			try
			{
				yield return SceneManager.LoadSceneAsync(BootScene, LoadSceneMode.Single);
			}
			finally
			{
				SceneManager.sceneLoaded -= RedirectSaves;
			}

			yield return WaitUntil(() => Object.FindFirstObjectByType<GameHost>() != null, "the game host");

			Host = Object.FindFirstObjectByType<GameHost>();

			yield return WaitUntil(() => IsEntered(Object.FindFirstObjectByType<MainMenuEntry>()), "the main menu");
		}

		public static void Click(Toolkit.VisualElement root, string buttonName, bool requireEnabled = true)
		{
			Toolkit.Button button = Toolkit.UQueryExtensions.Q<Toolkit.Button>(root, buttonName);

			Assert.That(button, Is.Not.Null, buttonName);

			if (requireEnabled)
			{
				Assert.That(button.enabledInHierarchy, Is.True, buttonName + " is disabled");
			}

			using Toolkit.NavigationSubmitEvent submit = Toolkit.NavigationSubmitEvent.GetPooled();

			submit.target = button;
			button.SendEvent(submit);
		}

		public IEnumerator SaveCampaign()
		{
			_ = Services.Campaign.SaveAsync();

			yield return WaitUntil(() => Services.Campaign.HasSave, "the campaign save");
		}

		public IEnumerator NewGameToDepot()
		{
			Click(Object.FindFirstObjectByType<MainMenuEntry>().Root, "entry-" + MainMenuEntry.NewGameEntryId);

			yield return WaitForDepot("the depot");
		}

		public IEnumerator ContinueToDepot()
		{
			Click(Object.FindFirstObjectByType<MainMenuEntry>().Root, "entry-" + MainMenuEntry.ContinueEntryId);

			yield return WaitForDepot("the depot after Continue");
		}

		public IEnumerator WaitForDepot(string description)
		{
			yield return WaitUntil(() => IsEntered(Depot), description);
		}

		public IEnumerator ReturnToMainMenu()
		{
			_ = Services.Navigator.GoToAsync(SceneIds.MainMenu);

			yield return WaitUntil(() => IsEntered(Object.FindFirstObjectByType<MainMenuEntry>()), "the main menu");
		}

		public IEnumerator CompleteInState(string contractId)
		{
			ContractRun run = Campaign.Contracts.Accept(contractId);

			Assert.That(run.CollectPackage(), Is.True);
			Assert.That(run.Depart(), Is.True);
			Assert.That(run.Deliver(), Is.True);

			_ = Campaign.Contracts.SettleAsync();

			yield return WaitUntil(() => Run == null, "the arranged settlement");
		}

		public IEnumerator AcceptInTerminal(string contractId, int clicks = 2)
		{
			DepotEntry depot = Depot;
			ContractSpec spec = null;

			foreach (ContractSpec candidate in Campaign.Contracts.GetAvailable())
			{
				if (candidate.Id == contractId)
				{
					spec = candidate;
				}
			}

			Assert.That(spec, Is.Not.Null, $"{contractId} is not available in the terminal.");

			Invoke(depot, "OpenTerminal");
			Invoke(depot, "OnSelected", spec);

			ContractTerminalView terminal = Object.FindFirstObjectByType<ContractTerminalView>();

			for (int click = 0; click < clicks; click++)
			{
				Click(terminal.Panel, ContractTerminalView.AcceptButtonName, requireEnabled: click == 0);
			}

			yield return WaitUntil(() => Run != null && Run.Spec.Id == contractId, "the accepted contract");
		}

		public IEnumerator CarryPackageToTheVanAndDepart()
		{
			yield return PickUpPackage();

			Assert.That(Run.Phase, Is.EqualTo(ContractPhase.PackageCollected));

			Interactable van = Get<Interactable>(Depot, "_van");

			Assert.That(van.IsAvailable, Is.True, "The van is not offered while carrying the package.");

			van.Press();

			yield return WaitUntil(() => IsEntered(Warehouse), "the warehouse");
		}

		public IEnumerator PickUpPackage()
		{
			CargoPackage package = Object.FindFirstObjectByType<CargoPackage>();
			PlayerSession player = Player;

			Assert.That(package, Is.Not.Null, "There is no package in the scene.");

			Teleport(player, package.transform.position + new Vector3(0f, 0.1f, -PickUpDistance));
			package.GetComponent<Interactable>().Press();

			yield return WaitUntil(() => player.Carry.IsCarrying, "the package in hand");
		}

		public IEnumerator ScanAndDeliver()
		{
			WarehouseEntry warehouse = Warehouse;
			PlayerSession player = Player;
			WarehouseFacts facts = Get<WarehouseFacts>(warehouse, "_facts");

			yield return PickUpPackage();

			Interactable lever = Get<Interactable>(Object.FindFirstObjectByType<PowerLever>(), "_interactable");
			lever.Press();

			Assert.That(facts.PowerOn, Is.True, "The breaker panel did not switch the power on.");

			PlacePackageOnScanner();

			yield return WaitUntil(() => facts.CargoScanned, "the cargo scan", ScanTimeoutSeconds);
			yield return PickUpPackage();

			Interactable terminal = Get<Interactable>(warehouse, "_deliveryPoint");

			PressAndHold(terminal);
			PressAndHold(terminal);

			yield return WaitUntil(() => ResultVisible, "the result panel");
		}

		public IEnumerator PowerUpTheBlackout()
		{
			WarehouseEntry warehouse = Warehouse;
			PlayerSession player = Player;
			WarehouseFacts facts = Get<WarehouseFacts>(warehouse, "_facts");

			Assert.That(facts.BatteryRequired, Is.True, "This contract has no blackout to power up.");

			Interactable battery = Get<Interactable>(Object.FindFirstObjectByType<BatteryCell>(), "_interactable");

			AssertDockPathClear(player.transform.position, DockAisleStart);
			AssertDockPathClear(DockAisleStart, DockAisleEnd);
			AssertDockPathClear(DockAisleEnd, battery.transform.position + Vector3.back * PickUpDistance);

			Teleport(player, battery.transform.position + new Vector3(0f, 0.1f, -PickUpDistance));
			battery.Press();

			yield return WaitUntil(() => player.Heavy.IsCarrying, "the battery in both hands");

			Interactable generator = Get<Interactable>(Object.FindFirstObjectByType<GeneratorSocket>(), "_interactable");

			Teleport(player, generator.transform.position + new Vector3(PickUpDistance, 0.1f, 0f));
			generator.Press();

			Assert.That(facts.BatteryInserted, Is.True, "The generator did not take the battery.");

			Get<Interactable>(Object.FindFirstObjectByType<PowerLever>(), "_interactable").Press();

			Assert.That(facts.PowerOn, Is.True, "The breaker panel did not switch the power on after the battery.");
		}

		private static void AssertDockPathClear(Vector3 from, Vector3 to)
		{
			from.y = 0f;
			to.y = 0f;
			Vector3 offset = to - from;

			bool blocked = Physics.CapsuleCast(
				from + Vector3.up * DockPathLowerHeight,
				from + Vector3.up * DockPathUpperHeight,
				DockPathRadius, offset.normalized, offset.magnitude,
				LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);

			Assert.That(blocked, Is.False, "The dock battery route is blocked by scene geometry.");
		}

		public IEnumerator StartContract(string contractId)
		{
			yield return BootToMainMenu();
			yield return NewGameToDepot();
			yield return AcceptInTerminal(contractId);
			yield return CarryPackageToTheVanAndDepart();
		}

		public void PlacePackageOnScanner()
		{
			PlayerSession player = Player;
			CargoScanner scanner = Object.FindFirstObjectByType<CargoScanner>();
			Collider plate = Get<Component>(scanner, "_plate").GetComponentInChildren<Collider>();
			CargoPackage package = Object.FindFirstObjectByType<CargoPackage>();

			Teleport(player, plate.bounds.center + new Vector3(-ScannerApproach, 0.1f, 0f));

			if (player.Carry.IsCarrying)
			{
				Assert.That(player.Carry.Drop(), Is.True);
			}

			Vector3 onPlate = plate.bounds.center + new Vector3(0f, plate.bounds.extents.y + ScannerPackageLift, 0f);

			MovePackage(package, onPlate);
		}

		public static void MovePackage(CargoPackage package, Vector3 position)
		{
			Rigidbody body = package.GetComponent<Rigidbody>();

			if (!body.isKinematic)
			{
				body.linearVelocity = Vector3.zero;
				body.angularVelocity = Vector3.zero;
			}

			body.position = position;
			package.transform.position = position;
		}

		public string NoticeText
		{
			get
			{
				PackageHudView hud = Object.FindFirstObjectByType<PackageHudView>();

				return hud.NoticeVisible ? hud.NoticeText : string.Empty;
			}
		}

		public string PromptText => Object.FindFirstObjectByType<InteractionPromptView>().PromptText;

		public bool ResultVisible => Get<ResultPanelView>(Warehouse, "_resultView").IsOpen;

		public string ResultText => Get<ResultPanelView>(Warehouse, "_resultView").Transcript;

		public void DamagePackage(double amount)
		{
			HealthCarrier carrier = Object.FindFirstObjectByType<CargoPackage>().GetComponentInChildren<HealthCarrier>();

			carrier.ReceiveDamage(new DamageRequest(amount, new DamageTypeId("impact"), "playmode-test", true));
		}

		public void KillPlayer()
		{
			HealthCarrier carrier = Player.GetComponentInChildren<HealthCarrier>();

			carrier.ReceiveDamage(new DamageRequest(10000d, new DamageTypeId("test"), "playmode-test", true));
		}

		public IEnumerator ReturnToDepotFromResult()
		{
			ResultPanelView result = Get<ResultPanelView>(Warehouse, "_resultView");

			Click(result.Panel, ResultPanelView.ReturnButtonName);
			Click(result.Panel, ResultPanelView.ReturnButtonName, requireEnabled: false);

			yield return WaitUntil(() => IsEntered(Depot), "the depot after the result");
		}

		private static bool IsEntered(Component entry)
		{
			return entry != null && Get<object>(entry, "_services") != null;
		}
	}
}
#endif
