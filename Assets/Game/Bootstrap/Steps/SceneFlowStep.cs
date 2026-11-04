using System.Threading;
using RD.Foundation;
using RD.SceneFlow;
using RD.Startup;
using UnityEngine;

namespace NightCourier.App
{
	public sealed class SceneFlowStep : StartupStep
	{
		public const string StepId = "scene-flow";

		private readonly GameServicesBuilder _builder;
		private readonly SceneCatalog _catalog;

		public SceneFlowStep(GameServicesBuilder builder, SceneCatalog catalog) : base(StepId)
		{
			_builder = builder;
			_catalog = catalog;
		}

		public override Awaitable StartAsync(CancellationToken cancellationToken = default)
		{
			_builder.SceneFlow = new RD.SceneFlow.SceneFlow(_catalog);

			return AwaitableExtensions.Completed;
		}
	}
}
