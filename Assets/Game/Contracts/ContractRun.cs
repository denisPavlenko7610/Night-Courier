using System;
using RD.Quests;
using RD.Timers;

namespace NightCourier.Contracts
{
	public sealed class ContractRun
	{
		private readonly QuestService _quests;
		private readonly CountdownTimer _deadline;

		private double _transitSeconds;
		private int _violationCount;

		public ContractRun(ContractSpec spec, QuestService quests)
		{
			Spec = spec ?? throw new ArgumentNullException(nameof(spec));
			_quests = quests ?? throw new ArgumentNullException(nameof(quests));
			_deadline = spec.HasTimeLimit ? new CountdownTimer(spec.TimeLimitSeconds) : null;
			Phase = ContractPhase.Accepted;
		}

		public ContractSpec Spec { get; }

		public ContractPhase Phase { get; private set; }

		public event Action<ContractPhase> PhaseChanged;

		public double TransitSeconds => _transitSeconds;

		public int ViolationCount => _violationCount;

		public bool HasDeadline => _deadline != null;

		public double RemainingSeconds => _deadline?.RemainingSeconds ?? 0d;

		public bool IsOverdue => _deadline != null && _deadline.State == CountdownTimerState.Completed;

		public bool IsFinished => Phase is ContractPhase.Completed or ContractPhase.Failed;

		public bool CollectPackage()
		{
			return Advance(ContractPhase.Accepted, ContractSignals.PackageCollected, ContractPhase.PackageCollected);
		}

		public bool Depart()
		{
			if (!Advance(ContractPhase.PackageCollected, ContractSignals.Departed, ContractPhase.InTransit))
			{
				return false;
			}

			_deadline?.Start();
			return true;
		}

		public bool Deliver()
		{
			if (!Advance(ContractPhase.InTransit, ContractSignals.PackageDelivered, ContractPhase.Delivered))
			{
				return false;
			}

			_deadline?.Pause();
			return true;
		}

		public void Tick(double deltaSeconds)
		{
			if (Phase != ContractPhase.InTransit)
			{
				return;
			}

			_transitSeconds += deltaSeconds;
			_deadline?.Tick(deltaSeconds);
		}

		public void RegisterViolation()
		{
			if (Phase == ContractPhase.InTransit)
			{
				_violationCount++;
			}
		}

		public ContractOutcome CaptureOutcome()
		{
			return new ContractOutcome(_violationCount, _transitSeconds, IsOverdue);
		}

		public bool Fail()
		{
			if (IsFinished || Phase == ContractPhase.Delivered)
			{
				return false;
			}

			_quests.TryAbandon(Spec.Id);
			SetPhase(ContractPhase.Failed);
			return true;
		}

		internal void MarkCompleted()
		{
			if (Phase != ContractPhase.Delivered)
			{
				throw new InvalidOperationException($"Contract '{Spec.Id}' cannot complete from phase {Phase}.");
			}

			SetPhase(ContractPhase.Completed);
		}

		private bool Advance(ContractPhase expected, string signal, ContractPhase next)
		{
			if (Phase != expected)
			{
				return false;
			}

			if (!_quests.Report(signal, Spec.Id))
			{
				throw new InvalidOperationException($"Quest state for contract '{Spec.Id}' is out of sync.");
			}

			SetPhase(next);
			return true;
		}

		private void SetPhase(ContractPhase phase)
		{
			Phase = phase;
			PhaseChanged?.Invoke(phase);
		}
	}
}
