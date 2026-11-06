using System;
using RD.Menus;
using RD.Settings;

namespace NightCourier.App
{
	public static class GameSettingsPage
	{
		public const string GraphicsSectionId = "graphics";
		public const string AudioSectionId = "audio";
		public const string ControlsSectionId = "controls";
		public const string GraphicsTitleKey = "settings.graphics";
		public const string AudioTitleKey = "settings.audio";
		public const string ControlsTitleKey = "settings.controls";
		public const string MasterVolumeLabelKey = "settings.master_volume";
		public const string MouseSensitivityLabelKey = "settings.mouse_sensitivity";

		private const float VolumeStep = 0.05f;
		private const float SensitivityStep = 0.05f;

		public static SettingsPage Create(ISettingsService settings)
		{
			return Create(settings, new PlayerPrefsMenuStore(GameSettings.StorePrefix));
		}

		public static SettingsPage Create(ISettingsService settings, IMenuValueStore store)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			SettingsPage page = new(store ?? throw new ArgumentNullException(nameof(store)));

			SettingsSection graphics = page.AddSection(GraphicsSectionId, GraphicsTitleKey);
			UnitySettingRows.AddQuality(graphics);
			UnitySettingRows.AddDisplayMode(graphics);
			UnitySettingRows.AddResolution(graphics);
			UnitySettingRows.AddVSync(graphics);
			UnitySettingRows.AddFrameRate(graphics, MenuProfile.Desktop);

			SettingsSection audio = page.AddSection(AudioSectionId, AudioTitleKey);
			audio.AddSlider(
				GameSettings.MasterVolume.Id,
				MasterVolumeLabelKey,
				0f,
				1f,
				GameSettings.DefaultMasterVolume,
				VolumeStep,
				SliderFormat.Percent,
				value => settings.Set(GameSettings.MasterVolume, value));

			SettingsSection controls = page.AddSection(ControlsSectionId, ControlsTitleKey);
			controls.AddSlider(
				GameSettings.MouseSensitivity.Id,
				MouseSensitivityLabelKey,
				GameSettings.MinMouseSensitivity,
				GameSettings.MaxMouseSensitivity,
				GameSettings.DefaultMouseSensitivity,
				SensitivityStep,
				SliderFormat.Number,
				value => settings.Set(GameSettings.MouseSensitivity, value));

			page.Load();

			return page;
		}
	}
}
