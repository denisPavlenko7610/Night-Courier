using System.Threading;
using RD.Foundation;
using RD.Pause;
using RD.Startup;
using UnityEngine;

namespace NightCourier.App
{
	public sealed class PauseStep : StartupStep
	{
		public const string StepId = "pause";

		private readonly GameServicesBuilder _builder;

		private PauseService _pauses;
		private UnityTimeScalePauseController _timeScale;

		public PauseStep(GameServicesBuilder builder) : base(StepId)
		{
			_builder = builder;
		}

		public override Awaitable StartAsync(CancellationToken cancellationToken = default)
		{
			_pauses = new PauseService();
			_timeScale = new UnityTimeScalePauseController(_pauses);
			_builder.Pause = _pauses;

			return AwaitableExtensions.Completed;
		}

		public override Awaitable StopAsync(CancellationToken cancellationToken = default)
		{
			_timeScale?.Dispose();
			_pauses?.Dispose();
			_timeScale = null;
			_pauses = null;

			return AwaitableExtensions.Completed;
		}
	}
}
