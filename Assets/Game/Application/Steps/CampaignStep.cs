using System.Threading;
using NightCourier.Contracts;
using RD.Save;
using RD.Startup;
using UnityEngine;

namespace NightCourier.App
{
	public sealed class CampaignStep : StartupStep
	{
		public const string StepId = "campaign";
		public const string SaveFolder = "Saves";

		private readonly GameServicesBuilder _builder;
		private readonly ContractCatalog _contracts;

		public CampaignStep(GameServicesBuilder builder, ContractCatalog contracts) : base(StepId)
		{
			_builder = builder;
			_contracts = contracts;
		}

		public override async Awaitable StartAsync(CancellationToken cancellationToken = default)
		{
			string root = System.IO.Path.Combine(Application.persistentDataPath, SaveFolder);
			CampaignRepository campaign = new(new SaveStore(root), _contracts.BuildBook());

			await campaign.RefreshAsync(cancellationToken);

			_builder.Campaign = campaign;
		}
	}
}
