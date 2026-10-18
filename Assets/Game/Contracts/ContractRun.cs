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
		private int _packageViolationCount;
		private int _deathCount;
		private int _shotsFired;
		private int _propertyDamageCount;
		private double _cargoIntegrity = 1d;

		public ContractRun(ContractSpec spec, QuestService quests)
		{
			Spec = spec ?? throw new ArgumentNullException(nameof(spec));
			_quests = quests ?? throw new ArgumentNullException(nameof(quests));
			Phase = ContractPhase.Accepted;

			if (spec.HasTimeLimit)
			{
				_deadline = new CountdownTimer(spec.TimeLimitSeconds);
				_deadline.Completed += OnDeadlineCompleted;
			}
		}

		public ContractSpec Spec { get; }

		public ContractPhase Phase { get; private set; }

		public event Action<ContractPhase> PhaseChanged;

		public event Action DeadlineExpired;

		public event Action<int> PackageViolationRegistered;

		public double TransitSeconds => _transitSeconds;

		public int ViolationCount => _violationCount;

		public int PackageViolationCount => _packageViolationCount;

		public int DeathCount => _deathCount;

		public int ShotsFired => _shotsFired;

		public int PropertyDamageCount => _propertyDamageCount;

		public double CargoIntegrity => _cargoIntegrity;

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
			if (Phase != ContractPhase.InTransit)
			{
				return;
			}

			_violationCount++;
			_packageViolationCount++;
			PackageViolationRegistered?.Invoke(_packageViolationCount);
		}

		public void RegisterShot()
		{
			if (Phase == ContractPhase.InTransit)
			{
				_shotsFired++;
			}
		}

		public void RecordCargoIntegrity(double fraction)
		{
			if (Phase == ContractPhase.InTransit)
			{
				_cargoIntegrity = Math.Clamp(fraction, 0d, 1d);
			}
		}

		public void RegisterDeath()
		{
			if (Phase == ContractPhase.InTransit)
			{
				_deathCount++;
			}
		}

		public void RegisterPropertyDamage()
		{
			if (Phase == ContractPhase.InTransit)
			{
				_violationCount++;
				_propertyDamageCount++;
			}
		}

		public ContractOutcome CaptureOutcome()
		{
			return new ContractOutcome(_violationCount, _transitSeconds, IsOverdue, _deathCount, _shotsFired, _propertyDamageCount, _cargoIntegrity);
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

		private void OnDeadlineCompleted()
		{
			DeadlineExpired?.Invoke();
		}

		private void SetPhase(ContractPhase phase)
		{
			Phase = phase;
			PhaseChanged?.Invoke(phase);
		}
	}
}
