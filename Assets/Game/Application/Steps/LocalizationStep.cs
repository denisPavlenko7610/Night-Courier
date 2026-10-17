using System.Collections.Generic;
using System.Threading;
using RD.Foundation;
using RD.Localization;
using RD.Settings;
using RD.Startup;
using UnityEngine;

namespace NightCourier.App
{
	public sealed class LocalizationStep : StartupStep
	{
		public const string StepId = "localization";

		private readonly GameServicesBuilder _builder;
		private readonly TextAsset _sourceTable;

		private LocalizationService _service;

		public LocalizationStep(GameServicesBuilder builder, TextAsset sourceTable) : base(StepId)
		{
			_builder = builder;
			_sourceTable = sourceTable;
		}

		public override IReadOnlyList<string> Dependencies => new[] { SettingsStep.StepId };

		public override async Awaitable StartAsync(CancellationToken cancellationToken = default)
		{
			_service = new LocalizationService(new JsonLocalizationProvider(_sourceTable));
			_service.IssueDetected += OnIssueDetected;
			_builder.Localization = _service;

			string requested = _builder.Settings.Get(GameSettings.Language);

			try
			{
				await _service.SetLocaleAsync(requested, cancellationToken);
			}
			catch (LocaleNotAvailableException exception)
			{
				UnityLogger.LogWarning(exception.Message);
			}
		}

		public override Awaitable StopAsync(CancellationToken cancellationToken = default)
		{
			if (_service != null)
			{
				_service.IssueDetected -= OnIssueDetected;
				_service.Dispose();
				_service = null;
			}

			return AwaitableExtensions.Completed;
		}

		private static void OnIssueDetected(LocalizationIssue issue)
		{
			UnityLogger.LogWarning($"Localization {issue.Kind} for key '{issue.Key}' in locale '{issue.Locale}'.");
		}
	}
}
