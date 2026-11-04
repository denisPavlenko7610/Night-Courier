using System;
using System.Threading;
using NightCourier.Contracts;
using NightCourier.Economy;
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
		private readonly UpgradeCatalog _upgrades;
		private readonly string _saveFolder;

		public CampaignStep(
			GameServicesBuilder builder,
			ContractCatalog contracts,
			UpgradeCatalog upgrades,
			string saveFolder = SaveFolder
		) : base(StepId)
		{
			if (string.IsNullOrWhiteSpace(saveFolder))
			{
				throw new ArgumentException("A save folder name is required.", nameof(saveFolder));
			}

			_builder = builder;
			_contracts = contracts;
			_upgrades = upgrades;
			_saveFolder = saveFolder;
		}

		public override async Awaitable StartAsync(CancellationToken cancellationToken = default)
		{
			string root = System.IO.Path.Combine(Application.persistentDataPath, _saveFolder);
			CampaignRepository campaign = new(new SaveStore(root), _contracts.BuildBook(), _upgrades.BuildBook());

			await campaign.RefreshAsync(cancellationToken);

			_builder.Campaign = campaign;
		}
	}
}
