using System.Threading;
using RD.Foundation;
using RD.Menus;
using RD.Settings;
using RD.Startup;
using UnityEngine;

namespace NightCourier.App
{
	public sealed class SettingsStep : StartupStep
	{
		public const string StepId = "settings";

		private readonly GameServicesBuilder _builder;

		private SettingsService _settings;
		private SettingsPage _page;

		public SettingsStep(GameServicesBuilder builder) : base(StepId)
		{
			_builder = builder;
		}

		public override Awaitable StartAsync(CancellationToken cancellationToken = default)
		{
			_settings = new SettingsService(new PlayerPrefsSettingsStore(GameSettings.StorePrefix));
			_settings.Changed += OnChanged;
			_builder.Settings = _settings;

			ApplyMasterVolume();

			_page = GameSettingsPage.Create(_settings);
			_builder.SettingsPage = _page;

			return AwaitableExtensions.Completed;
		}

		public override Awaitable StopAsync(CancellationToken cancellationToken = default)
		{
			_page?.Dispose();
			_page = null;

			if (_settings != null)
			{
				_settings.Changed -= OnChanged;
				_settings.Flush();
				_settings = null;
			}

			return AwaitableExtensions.Completed;
		}

		private void OnChanged(SettingChanged change)
		{
			if (change.Id == GameSettings.MasterVolume.Id)
			{
				ApplyMasterVolume();
				_settings.Flush();
			}
		}

		private void ApplyMasterVolume()
		{
			AudioListener.volume = _settings.Get(GameSettings.MasterVolume);
		}
	}
}
