using System;
using NightCourier.Contracts;
using RD.Tutorials;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class FirstDeliveryTraining : IDisposable
	{
		public const string ContractId = "contract.standard-delivery";
		public const string CaptionKey = "training.caption";
		private const string DepotId = "training.depot";
		private const string WarehouseId = "training.warehouse";
		private const string Accept = "accept";
		private const string Collect = "collect";
		private const string Depart = "depart";
		private const string Power = "power";
		private const string Scan = "scan";
		private const string Retrieve = "retrieve";
		private const string Deliver = "deliver";

		private readonly TutorialService _service = new(new MemoryTutorialProgressStore());
		private readonly TutorialDefinition _definition;

		public FirstDeliveryTraining(bool depot)
		{
			string[] ids = depot ? new[] { Accept, Collect, Depart } : new[] { Power, Scan, Retrieve, Deliver };
			TutorialStep[] steps = new TutorialStep[ids.Length];

			for (int index = 0; index < ids.Length; index++)
			{
				steps[index] = new TutorialStep(ids[index]).WithCompletionMode(TutorialStepCompletionMode.GameAction);
			}

			_definition = TutorialDefinition.CreateRuntime(depot ? DepotId : WarehouseId, false, steps);
			_service.Start(_definition);
		}

		public int StepNumber => _service.TryGetCurrentStep(out TutorialStepState step) ? step.Index + 1 : 0;
		public int StepCount => _definition.Steps.Count;

		public void ObserveDepot(ContractRun run)
		{
			if (run == null || run.Spec.Id != ContractId)
			{
				return;
			}

			_service.TryCompleteStep(Accept);

			if (run.Phase != ContractPhase.Accepted)
			{
				_service.TryCompleteStep(Collect);
			}

			if (run.Phase == ContractPhase.InTransit)
			{
				_service.TryCompleteStep(Depart);
			}
		}

		public void ObserveLocation(WarehouseProgress progress, bool delivered)
		{
			if (progress.PowerOn)
			{
				_service.TryCompleteStep(Power);
			}
			if (progress.CargoScanned)
			{
				_service.TryCompleteStep(Scan);
			}
			if (progress.CargoScanned && progress.CarryingPackage)
			{
				_service.TryCompleteStep(Retrieve);
			}
			if (delivered)
			{
				_service.TryCompleteStep(Deliver);
			}
		}

		public void Dispose()
		{
			_service.Cancel();
			UnityEngine.Object.Destroy(_definition);
		}
	}
}
