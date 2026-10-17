using System;
using NightCourier.Controls;
using RD.Localization;
using RD.Pause;
using RD.SceneFlow;
using RD.Settings;

namespace NightCourier.App
{
	public sealed class GameServicesBuilder
	{
		public ISettingsService Settings { get; set; }

		public ILocalizationService Localization { get; set; }

		public CampaignRepository Campaign { get; set; }

		public ISceneFlow SceneFlow { get; set; }

		public GameInput Input { get; set; }

		public IPauseService Pause { get; set; }

		public GameServices Build(IGameNavigator navigator)
		{
			if (SceneFlow == null)
			{
				throw new InvalidOperationException("Scene flow has not been started.");
			}

			return new GameServices(navigator, Settings, Localization, Campaign, Input, Pause);
		}
	}
}
