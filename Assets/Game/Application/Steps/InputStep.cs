using System;
using System.Threading;
using NightCourier.Controls;
using RD.Foundation;
using RD.Startup;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCourier.App
{
	public sealed class InputStep : StartupStep
	{
		public const string StepId = "input";

		private readonly GameServicesBuilder _builder;
		private readonly InputActionAsset _actions;

		private GameInput _input;

		public InputStep(GameServicesBuilder builder, InputActionAsset actions) : base(StepId)
		{
			_builder = builder;
			_actions = actions;
		}

		public override Awaitable StartAsync(CancellationToken cancellationToken = default)
		{
			_input = new GameInput(_actions ?? throw new InvalidOperationException("The input actions asset is not assigned."));
			_builder.Input = _input;

			return AwaitableExtensions.Completed;
		}

		public override Awaitable StopAsync(CancellationToken cancellationToken = default)
		{
			_input?.Dispose();
			_input = null;

			return AwaitableExtensions.Completed;
		}
	}
}
