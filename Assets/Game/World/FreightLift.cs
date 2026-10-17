using System;
using NightCourier.Player;
using RD.Mechanics.MovingPlatform;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class FreightLift : MonoBehaviour
	{
		private const int LowerStop = 0;
		private const int UpperStop = 1;

		[SerializeField] private Elevator _elevator;
		[SerializeField] private Interactable _console;

		private WarehouseFacts _facts;

		public void Bind(WarehouseFacts facts)
		{
			Unbind();

			_facts = facts ?? throw new ArgumentNullException(nameof(facts));

			_facts.Changed += Refresh;
			_console.Interacted += OnInteracted;
			_elevator.RideStarted += OnRideChanged;
			_elevator.RideCompleted += OnRideChanged;

			Refresh();
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_facts != null)
			{
				_facts.Changed -= Refresh;
				_facts = null;
			}

			if (_console != null)
			{
				_console.Interacted -= OnInteracted;
			}

			if (_elevator != null)
			{
				_elevator.RideStarted -= OnRideChanged;
				_elevator.RideCompleted -= OnRideChanged;
			}
		}

		private void Refresh()
		{
			_console.SetAvailable(_facts.PowerOn && !_elevator.IsMoving);
		}

		private void OnRideChanged(int stop)
		{
			Refresh();
		}

		private void OnInteracted()
		{
			_elevator.MoveTo(_elevator.CurrentStop == LowerStop ? UpperStop : LowerStop);
		}
	}
}
