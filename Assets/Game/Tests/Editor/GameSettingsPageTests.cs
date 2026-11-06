#if UNITY_INCLUDE_TESTS
using NightCourier.App;
using NUnit.Framework;
using RD.Menus;
using RD.Settings;

namespace NightCourier.Tests
{
	public sealed class GameSettingsPageTests
	{
		private const float VolumeChoice = 0.4f;
		private const float StoredSensitivity = 2f;
		private const float Tolerance = 0.0001f;

		private SettingsService _settings;
		private MemoryMenuStore _store;
		private SettingsPage _page;

		[SetUp]
		public void CreatePage()
		{
			_settings = new SettingsService(new MemorySettingsStore());
			_store = new MemoryMenuStore();
		}

		[TearDown]
		public void DisposePage()
		{
			_page?.Dispose();
		}

		[Test]
		public void Create_AddsGraphicsAudioAndControlsSections()
		{
			_page = GameSettingsPage.Create(_settings, _store);

			Assert.That(_page.IsLoaded, Is.True);
			Assert.That(_page.Sections.Count, Is.EqualTo(3));
			Assert.That(_page.Sections[0].Id, Is.EqualTo(GameSettingsPage.GraphicsSectionId));
			Assert.That(_page.Sections[1].Id, Is.EqualTo(GameSettingsPage.AudioSectionId));
			Assert.That(_page.Sections[2].Id, Is.EqualTo(GameSettingsPage.ControlsSectionId));
			Assert.That(_page.TryGetRow(GameSettings.MasterVolume.Id, out SliderRow _), Is.True);
			Assert.That(_page.TryGetRow(GameSettings.MouseSensitivity.Id, out SliderRow _), Is.True);
			Assert.That(_page.TryGetRow(UnitySettingRows.QualityId, out ChoiceRow _), Is.True);
			Assert.That(_page.TryGetRow(UnitySettingRows.ResolutionId, out ChoiceRow _), Is.True);
		}

		[Test]
		public void Apply_WritesTheSliderValuesToTheSettingsService()
		{
			_page = GameSettingsPage.Create(_settings, _store);

			_page.TryGetRow(GameSettings.MasterVolume.Id, out SliderRow volume);

			volume.Value = VolumeChoice;

			Assert.That(_settings.Get(GameSettings.MasterVolume), Is.EqualTo(GameSettings.DefaultMasterVolume).Within(Tolerance));

			_page.Apply();

			Assert.That(_settings.Get(GameSettings.MasterVolume), Is.EqualTo(VolumeChoice).Within(Tolerance));
			Assert.That(_store.TryRead(GameSettings.MasterVolume.Id, out string stored), Is.True);
			Assert.That(float.Parse(stored, System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(VolumeChoice).Within(Tolerance));
		}

		[Test]
		public void Revert_LeavesTheSettingsServiceUntouched()
		{
			_page = GameSettingsPage.Create(_settings, _store);

			_page.TryGetRow(GameSettings.MouseSensitivity.Id, out SliderRow sensitivity);

			sensitivity.Value = GameSettings.MaxMouseSensitivity;
			_page.Revert();

			Assert.That(_settings.Get(GameSettings.MouseSensitivity), Is.EqualTo(GameSettings.DefaultMouseSensitivity).Within(Tolerance));
			Assert.That(sensitivity.Value, Is.EqualTo(GameSettings.DefaultMouseSensitivity).Within(Tolerance));
		}

		[Test]
		public void Load_AppliesStoredValuesToTheSettingsService()
		{
			_store.Write(GameSettings.MouseSensitivity.Id, StoredSensitivity.ToString(System.Globalization.CultureInfo.InvariantCulture));

			_page = GameSettingsPage.Create(_settings, _store);

			Assert.That(_settings.Get(GameSettings.MouseSensitivity), Is.EqualTo(StoredSensitivity).Within(Tolerance));
		}
	}
}
#endif
