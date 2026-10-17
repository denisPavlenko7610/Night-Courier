using System;
using NightCourier.Controls;
using RD.Localization;
using RD.Pause;
using RD.Settings;

namespace NightCourier.App
{
	public sealed class GameServices
	{
		public GameServices(
			IGameNavigator navigator,
			ISettingsService settings,
			ILocalizationService localization,
			CampaignRepository campaign,
			GameInput input,
			IPauseService pause
		)
		{
			Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
			Settings = settings ?? throw new ArgumentNullException(nameof(settings));
			Localization = localization ?? throw new ArgumentNullException(nameof(localization));
			Campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
			Input = input ?? throw new ArgumentNullException(nameof(input));
			Pause = pause ?? throw new ArgumentNullException(nameof(pause));
		}

		public IGameNavigator Navigator { get; }

		public ISettingsService Settings { get; }

		public ILocalizationService Localization { get; }

		public CampaignRepository Campaign { get; }

		public GameInput Input { get; }

		public IPauseService Pause { get; }
	}
}
