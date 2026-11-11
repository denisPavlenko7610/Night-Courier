#if UNITY_INCLUDE_TESTS
using System.Collections;
using NightCourier.App;
using NightCourier.UI;
using NUnit.Framework;
using RD.Menus;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Toolkit = UnityEngine.UIElements;

namespace NightCourier.PlayModeTests
{
	[Category("Integration")]
	public sealed class MenuPlayModeTests
	{
		private const int TestTimeoutMilliseconds = 120000;
		private const float ChosenVolume = 0.35f;
		private const float Tolerance = 0.001f;
		private const string AudioTab = "tab-audio";
		private const string SliderControl = "control";
		private const string SettingsApply = "settings-apply";
		private const string SettingsBack = "settings-back";

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
		public IEnumerator Continue_IsDisabledWithoutASave_AndEnabledAfterOne()
		{
			yield return _driver.BootToMainMenu();

			MainMenuEntry menu = Object.FindFirstObjectByType<MainMenuEntry>();

			Assert.That(IsEntryEnabled(menu.Root, MainMenuEntry.ContinueEntryId), Is.False);

			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.SaveCampaign();
			yield return _driver.ReturnToMainMenu();

			menu = Object.FindFirstObjectByType<MainMenuEntry>();

			Assert.That(IsEntryEnabled(menu.Root, MainMenuEntry.ContinueEntryId), Is.True);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator SavingAgain_ContinueRestoresTheLatestProgress()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.SaveCampaign();

			long earlierCredits = _driver.Campaign.Credits;

			yield return _driver.CompleteInState(GameDriver.Fragile);
			yield return _driver.SaveCampaign();

			long latestCredits = _driver.Campaign.Credits;

			Assert.That(latestCredits, Is.GreaterThan(earlierCredits));

			yield return _driver.ReturnToMainMenu();
			yield return _driver.ContinueToDepot();

			Assert.That(_driver.Campaign.Credits, Is.EqualTo(latestCredits));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator NewGameOverASave_AsksFirst_CancelKeepsTheSave_ConfirmStartsOver()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.SaveCampaign();

			long credits = _driver.Campaign.Credits;

			Assert.That(credits, Is.Positive);

			yield return _driver.ReturnToMainMenu();

			MainMenuEntry menu = Object.FindFirstObjectByType<MainMenuEntry>();

			GameDriver.Click(menu.Root, "entry-" + MainMenuEntry.NewGameEntryId);

			Assert.That(menu.Dialog.IsOpen, Is.True);

			GameDriver.Click(menu.Root, ConfirmDialog.CancelButtonName);

			Assert.That(menu.Dialog.IsOpen, Is.False);
			Assert.That(menu.Menu.Entries.Count, Is.EqualTo(4));
			Assert.That(IsEntryEnabled(menu.Root, MainMenuEntry.ContinueEntryId), Is.True);

			GameDriver.Click(menu.Root, "entry-" + MainMenuEntry.NewGameEntryId);
			GameDriver.Click(menu.Root, ConfirmDialog.ConfirmButtonName);

			yield return _driver.WaitForDepot("the depot after the confirmed new game");

			Assert.That(_driver.Campaign.Credits, Is.Zero);
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Settings_ApplyingTheVolume_ReachesTheListenerAndPersistsInTheSettingsService()
		{
			yield return _driver.BootToMainMenu();

			MainMenuEntry menu = Object.FindFirstObjectByType<MainMenuEntry>();
			SettingsPage page = _driver.Services.SettingsPage;

			page.TryGetRow(GameSettings.MasterVolume.Id, out SliderRow row);

			float original = row.Value;

			try
			{
				GameDriver.Click(menu.Root, "entry-" + MainMenuEntry.SettingsEntryId);

				yield return GameDriver.WaitUntil(() => menu.Navigator.CurrentScreenId == MainMenuEntry.SettingsScreenId, "the settings screen");

				GameDriver.Click(menu.Root, AudioTab);

				Toolkit.Slider slider = Toolkit.UQueryExtensions.Q<Toolkit.Slider>(menu.Root, SliderControl);

				slider.value = ChosenVolume;

				Assert.That(page.HasChanges, Is.True);
				Assert.That(AudioListener.volume, Is.EqualTo(original).Within(Tolerance), "The draft reached the engine before Apply.");

				GameDriver.Click(menu.Root, SettingsApply);

				Assert.That(page.HasChanges, Is.False);
				Assert.That(AudioListener.volume, Is.EqualTo(ChosenVolume).Within(Tolerance));
				Assert.That(_driver.Services.Settings.Get(GameSettings.MasterVolume), Is.EqualTo(ChosenVolume).Within(Tolerance));

				slider.value = original;

				GameDriver.Click(menu.Root, SettingsApply);
				GameDriver.Click(menu.Root, SettingsBack);

				yield return GameDriver.WaitUntil(() => menu.Navigator.CurrentScreenId == MainMenuEntry.MainScreenId, "the main screen after Back");
			}
			finally
			{
				row.Value = original;
				page.Apply();
			}

			Assert.That(AudioListener.volume, Is.EqualTo(original).Within(Tolerance));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Back_DiscardsUnappliedEdits()
		{
			yield return _driver.BootToMainMenu();

			MainMenuEntry menu = Object.FindFirstObjectByType<MainMenuEntry>();
			SettingsPage page = _driver.Services.SettingsPage;

			page.TryGetRow(GameSettings.MouseSensitivity.Id, out SliderRow row);

			float original = row.Value;

			GameDriver.Click(menu.Root, "entry-" + MainMenuEntry.SettingsEntryId);

			yield return GameDriver.WaitUntil(() => menu.Navigator.CurrentScreenId == MainMenuEntry.SettingsScreenId, "the settings screen");

			row.Value = GameSettings.MaxMouseSensitivity;

			Assert.That(page.HasChanges, Is.True);

			GameDriver.Click(menu.Root, SettingsBack);

			yield return GameDriver.WaitUntil(() => menu.Navigator.CurrentScreenId == MainMenuEntry.MainScreenId, "the main screen after Back");

			Assert.That(page.HasChanges, Is.False);
			Assert.That(row.Value, Is.EqualTo(original).Within(Tolerance));
			Assert.That(_driver.Services.Settings.Get(GameSettings.MouseSensitivity), Is.EqualTo(original).Within(Tolerance));
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator PauseMenu_NavigatesToSettingsAndBack_AndEscapeClosesTheConfirmFirst()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();

			PauseMenu pause = Object.FindFirstObjectByType<PauseMenu>();

			pause.Open();

			yield return GameDriver.WaitUntil(() => pause.Navigator.CurrentScreenId == PauseMenu.MainScreenId, "the pause screen");

			Assert.That(Time.timeScale, Is.EqualTo(0f));
			Assert.That(
				Toolkit.UQueryExtensions.Q<Toolkit.Button>(pause.Root, "entry-" + PauseMenu.LeaveEntryId),
				Is.Null,
				"The depot offers no abort without a lost package.");

			GameDriver.Click(pause.Root, "entry-" + PauseMenu.SettingsEntryId);

			yield return GameDriver.WaitUntil(() => pause.Navigator.CurrentScreenId == PauseMenu.SettingsScreenId, "the pause settings");

			GameDriver.Invoke(pause, "OnCancel");

			yield return GameDriver.WaitUntil(() => pause.Navigator.CurrentScreenId == PauseMenu.MainScreenId, "the pause screen after Escape");

			GameDriver.Click(pause.Root, "entry-" + PauseMenu.MainMenuEntryId);

			Assert.That(pause.Dialog.IsOpen, Is.True);

			GameDriver.Invoke(pause, "OnCancel");

			Assert.That(pause.Dialog.IsOpen, Is.False);
			Assert.That(pause.IsOpen, Is.True, "Escape closed the pause menu together with the confirmation.");

			GameDriver.Invoke(pause, "OnCancel");

			Assert.That(pause.IsOpen, Is.False);

			yield return GameDriver.WaitUntil(() => Time.timeScale == 1f, "the game to resume");
		}

		private static bool IsEntryEnabled(Toolkit.VisualElement root, string entryId)
		{
			Toolkit.Button button = Toolkit.UQueryExtensions.Q<Toolkit.Button>(root, "entry-" + entryId);

			return button != null && button.enabledSelf;
		}
	}
}
#endif
