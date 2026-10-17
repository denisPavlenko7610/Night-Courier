using System;
using NightCourier.World;
using RD.Conditions;
using RD.DependencyInjection;

namespace NightCourier.App
{
	public static class GameComposition
	{
		public static Container Build(GameServicesBuilder startup, IGameNavigator navigator)
		{
			if (startup == null)
			{
				throw new ArgumentNullException(nameof(startup));
			}

			if (startup.SceneFlow == null)
			{
				throw new InvalidOperationException("Scene flow has not been started.");
			}

			ContainerBuilder builder = new();

			builder.RegisterInstance(navigator ?? throw new ArgumentNullException(nameof(navigator)));
			builder.RegisterInstance(startup.Settings ?? throw new InvalidOperationException("Settings have not been started."));
			builder.RegisterInstance(startup.Localization ?? throw new InvalidOperationException("Localization has not been started."));
			builder.RegisterInstance(startup.Campaign ?? throw new InvalidOperationException("The campaign has not been started."));
			builder.RegisterInstance(startup.Input ?? throw new InvalidOperationException("Input has not been started."));
			builder.RegisterInstance(startup.Pause ?? throw new InvalidOperationException("Pause has not been started."));
			builder.Register<GameServices>(Lifetime.Singleton);

			RegisterWarehouse(builder);

			return builder.Build();
		}

		public static void RegisterWarehouse(ContainerBuilder builder)
		{
			if (builder == null)
			{
				throw new ArgumentNullException(nameof(builder));
			}

			builder.Register<WarehouseFacts>(Lifetime.Scoped);
			builder.RegisterFactory(_ => WarehouseConditions.CreateEngine(), Lifetime.Scoped);
		}
	}
}
