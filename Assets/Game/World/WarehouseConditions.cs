using System;
using System.Collections.Generic;
using RD.Conditions;

namespace NightCourier.World
{
	public enum WarehouseFact
	{
		PowerOn = 0,
		CargoScanned = 1
	}

	public sealed class WarehouseFactEvaluator : ConditionEvaluator<WarehouseFact>
	{
		public const string PowerOffReason = "warehouse.power_off";
		public const string CargoNotScannedReason = "warehouse.cargo_not_scanned";

		public override ConditionTypeId Type => WarehouseConditions.FactType;

		protected override LeafConditionResult Evaluate(ConditionLeaf<WarehouseFact> condition, ConditionContext context)
		{
			WarehouseFacts facts = context.GetRequired<WarehouseFacts>(WarehouseConditions.FactsKey);

			return condition.Data switch
			{
				WarehouseFact.PowerOn => facts.PowerOn
					? LeafConditionResult.Satisfied()
					: LeafConditionResult.NotSatisfied(PowerOffReason),
				WarehouseFact.CargoScanned => facts.CargoScanned
					? LeafConditionResult.Satisfied()
					: LeafConditionResult.NotSatisfied(CargoNotScannedReason),
				_ => throw new ArgumentOutOfRangeException(nameof(condition), condition.Data, "Unknown warehouse fact.")
			};
		}
	}

	public static class WarehouseConditions
	{
		public static readonly ConditionTypeId FactType = new("warehouse.fact");
		public static readonly ConditionContextKey FactsKey = new("warehouse.facts");

		private static readonly ConditionId PowerLeafId = new("warehouse.leaf.power_on");
		private static readonly ConditionId ScanLeafId = new("warehouse.leaf.cargo_scanned");
		private static readonly ConditionId MaintenanceDoorId = new("warehouse.rule.maintenance_door");
		private static readonly ConditionId ScannerHatchId = new("warehouse.rule.scanner_hatch");

		public static ConditionEngine CreateEngine()
		{
			return new ConditionEngine(new IConditionEvaluator[] { new WarehouseFactEvaluator() });
		}

		public static ConditionContext CreateContext(WarehouseFacts facts)
		{
			if (facts == null)
			{
				throw new ArgumentNullException(nameof(facts));
			}

			return new ConditionContext(
				facts,
				new[] { new KeyValuePair<ConditionContextKey, object>(FactsKey, facts) }
			);
		}

		public static ConditionRule MaintenanceDoor()
		{
			return ConditionRules.All(MaintenanceDoorId, PowerOn());
		}

		public static ConditionRule ScannerHatch()
		{
			return ConditionRules.All(ScannerHatchId, PowerOn(), CargoScanned());
		}

		private static ConditionRule PowerOn()
		{
			return ConditionRules.Leaf(PowerLeafId, FactType, WarehouseFact.PowerOn);
		}

		private static ConditionRule CargoScanned()
		{
			return ConditionRules.Leaf(ScanLeafId, FactType, WarehouseFact.CargoScanned);
		}
	}
}
