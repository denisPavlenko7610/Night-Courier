using System;
using RD.Localization;
using RD.Menus;

namespace NightCourier.UI
{
	public static class GameMenuText
	{
		public const string QualityKey = "settings.quality";
		public const string DisplayModeKey = "settings.display_mode";
		public const string ResolutionKey = "settings.resolution";
		public const string VSyncKey = "settings.vsync";
		public const string FrameRateKey = "settings.frame_rate";

		public static IMenuTextProvider Create(ILocalizationService localization)
		{
			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			return new FuncMenuTextProvider(key => localization.TryGet(MapLabel(key), out string text) ? text : null);
		}

		public static string MapLabel(string key)
		{
			return key switch
			{
				UnitySettingRows.QualityLabel => QualityKey,
				UnitySettingRows.DisplayModeLabel => DisplayModeKey,
				UnitySettingRows.ResolutionLabel => ResolutionKey,
				UnitySettingRows.VSyncLabel => VSyncKey,
				UnitySettingRows.FrameRateLabel => FrameRateKey,
				_ => key
			};
		}
	}
}
