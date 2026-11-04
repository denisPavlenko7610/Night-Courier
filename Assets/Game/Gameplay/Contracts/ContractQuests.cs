using System;
using RD.Quests;

namespace NightCourier.Contracts
{
	public static class ContractSignals
	{
		public const string PackageCollected = "contract.package-collected";
		public const string Departed = "contract.departed";
		public const string PackageDelivered = "contract.package-delivered";
	}

	public static class ContractQuests
	{
		public const string CollectStepId = "collect";
		public const string DepartStepId = "depart";
		public const string DeliverStepId = "deliver";

		public static QuestDefinition Build(ContractSpec spec)
		{
			if (spec == null)
			{
				throw new ArgumentNullException(nameof(spec));
			}

			return new QuestDefinition(
				spec.Id,
				new[]
				{
					Step(CollectStepId, ContractSignals.PackageCollected, spec.Id),
					Step(DepartStepId, ContractSignals.Departed, spec.Id),
					Step(DeliverStepId, ContractSignals.PackageDelivered, spec.Id)
				},
				spec.Prerequisites,
				spec.IsRepeatable
			);
		}

		private static QuestStepDefinition Step(string stepId, string signal, string contractId)
		{
			return new QuestStepDefinition(
				stepId,
				new[] { new QuestObjectiveDefinition(stepId, signal, 1d, contractId) }
			);
		}
	}
}
