using RD.Settings;
using UnityEngine;

namespace NightCourier.App
{
	public static class GameSettings
	{
		public const string StorePrefix = "nightcourier.";
		public const string InputBindingsKey = "nightcourier.input-bindings";
		public const string DefaultLocale = "en";
		public const float DefaultMasterVolume = 0.8f;
		public const float MinMouseSensitivity = 0.1f;
		public const float MaxMouseSensitivity = 3f;
		public const float DefaultMouseSensitivity = 1f;

		public static readonly SettingKey<float> MasterVolume = new(
			"audio.master-volume",
			DefaultMasterVolume,
			SettingCodecs.Single,
			Mathf.Clamp01
		);

		public static readonly SettingKey<float> MouseSensitivity = new(
			"input.mouse-sensitivity",
			DefaultMouseSensitivity,
			SettingCodecs.Single,
			value => Mathf.Clamp(value, MinMouseSensitivity, MaxMouseSensitivity)
		);

		public static readonly SettingKey<string> Language = new(
			"ui.language",
			DefaultLocale,
			SettingCodecs.String
		);
	}
}
