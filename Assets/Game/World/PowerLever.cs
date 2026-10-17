using System;
using NightCourier.Player;
using RD.Mechanics.Lever;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class PowerLever : MonoBehaviour
	{
		[SerializeField] private Interactable _interactable;
		[SerializeField] private Lever _lever;

		private WarehouseFacts _facts;

		public void Bind(WarehouseFacts facts)
		{
			Unbind();

			_facts = facts ?? throw new ArgumentNullException(nameof(facts));

			_interactable.Interacted += OnInteracted;
			_lever.StateChanged += OnStateChanged;
			_facts.Changed += Refresh;

			Refresh();
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_interactable != null)
			{
				_interactable.Interacted -= OnInteracted;
			}

			if (_lever != null)
			{
				_lever.StateChanged -= OnStateChanged;
			}

			if (_facts != null)
			{
				_facts.Changed -= Refresh;
				_facts = null;
			}
		}

		private void Refresh()
		{
			_interactable.SetAvailable(_facts.LeverReady);
		}

		private void OnInteracted()
		{
			_lever.SetState(true);
		}

		private void OnStateChanged(bool on)
		{
			_facts.SetPower(on);
		}
	}
}
