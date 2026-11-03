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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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

		public static string SaveRoot => Path.Combine(Application.persistentDataPath, CampaignStep.SaveFolder);

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

		public IEnumerator BootToMainMenu()
		{
			yield return SceneManager.LoadSceneAsync(BootScene, LoadSceneMode.Single);
			yield return WaitUntil(() => Object.FindFirstObjectByType<GameHost>() != null, "the game host");

			Host = Object.FindFirstObjectByType<GameHost>();

			yield return WaitUntil(() => IsEntered(Object.FindFirstObjectByType<MainMenuEntry>()), "the main menu");
		}

		public IEnumerator NewGameToDepot()
		{
			Get<Button>(Object.FindFirstObjectByType<MainMenuEntry>(), "_newGameButton").onClick.Invoke();

			yield return WaitForDepot("the depot");
		}

		public IEnumerator ContinueToDepot()
		{
			Get<Button>(Object.FindFirstObjectByType<MainMenuEntry>(), "_continueButton").onClick.Invoke();

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

			Button accept = Get<Button>(depot, "_actionButton");

			for (int click = 0; click < clicks; click++)
			{
				accept.onClick.Invoke();
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

			CargoScanner scanner = Object.FindFirstObjectByType<CargoScanner>();
			Collider plate = Get<Component>(scanner, "_plate").GetComponentInChildren<Collider>();
			CargoPackage package = Object.FindFirstObjectByType<CargoPackage>();

			Teleport(player, plate.bounds.center + new Vector3(-ScannerApproach, 0.1f, 0f));
			Assert.That(player.Carry.Drop(), Is.True);

			Vector3 onPlate = plate.bounds.center + new Vector3(0f, plate.bounds.extents.y + ScannerPackageLift, 0f);
			Rigidbody body = package.GetComponent<Rigidbody>();

			body.linearVelocity = Vector3.zero;
			body.position = onPlate;
			package.transform.position = onPlate;

			yield return WaitUntil(() => facts.CargoScanned, "the cargo scan", ScanTimeoutSeconds);
			yield return PickUpPackage();

			Interactable terminal = Get<Interactable>(warehouse, "_deliveryPoint");

			PressAndHold(terminal);
			PressAndHold(terminal);

			yield return WaitUntil(() => ResultVisible, "the result panel");
		}

		public bool ResultVisible => Get<GameObject>(Warehouse, "_resultPanel").activeSelf;

		public string ResultText => Get<Text>(Warehouse, "_breakdownText").text;

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
			Button button = Get<Button>(Warehouse, "_returnButton");

			button.onClick.Invoke();
			button.onClick.Invoke();

			yield return WaitUntil(() => IsEntered(Depot), "the depot after the result");
		}

		private static bool IsEntered(Component entry)
		{
			return entry != null && Get<object>(entry, "_services") != null;
		}
	}
}
#endif
