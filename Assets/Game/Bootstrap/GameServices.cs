using System;
using NightCourier.Controls;
using RD.Audio;
using RD.Localization;
using RD.Menus;
using RD.Pause;
using RD.Settings;

namespace NightCourier.App
{
	public sealed class GameServices
	{
		public GameServices(
			IGameNavigator navigator,
			ISettingsService settings,
			SettingsPage settingsPage,
			ILocalizationService localization,
			CampaignRepository campaign,
			GameInput input,
			IPauseService pause,
			AudioService audio
		)
		{
			Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
			Settings = settings ?? throw new ArgumentNullException(nameof(settings));
			SettingsPage = settingsPage ?? throw new ArgumentNullException(nameof(settingsPage));
			Localization = localization ?? throw new ArgumentNullException(nameof(localization));
			Campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
			Input = input ?? throw new ArgumentNullException(nameof(input));
			Pause = pause ?? throw new ArgumentNullException(nameof(pause));
			Audio = audio ?? throw new ArgumentNullException(nameof(audio));
		}

		public IGameNavigator Navigator { get; }

		public ISettingsService Settings { get; }

		public SettingsPage SettingsPage { get; }

		public ILocalizationService Localization { get; }

		public CampaignRepository Campaign { get; }

		public GameInput Input { get; }

		public IPauseService Pause { get; }

		public AudioService Audio { get; }
	}
}
