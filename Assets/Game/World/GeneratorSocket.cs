using System;
using NightCourier.Player;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class GeneratorSocket : MonoBehaviour
	{
		[SerializeField] private Interactable _interactable;
		[SerializeField] private GameObject _batteryVisual;

		private WarehouseFacts _facts;
		private PlayerHeavyCarry _heavy;

		public void Bind(WarehouseFacts facts, PlayerHeavyCarry heavy)
		{
			Unbind();

			_facts = facts ?? throw new ArgumentNullException(nameof(facts));
			_heavy = heavy != null ? heavy : throw new ArgumentNullException(nameof(heavy));

			_facts.Changed += Refresh;
			_heavy.HeldChanged += Refresh;
			_interactable.Interacted += OnInteracted;
			_batteryVisual.SetActive(false);

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

			if (_heavy != null)
			{
				_heavy.HeldChanged -= Refresh;
				_heavy = null;
			}

			if (_interactable != null)
			{
				_interactable.Interacted -= OnInteracted;
			}
		}

		private void Refresh()
		{
			bool holdingBattery = _heavy.Carried != null && _heavy.Carried.GetComponent<BatteryCell>() != null;

			_interactable.SetAvailable(_facts.BatteryRequired && !_facts.BatteryInserted && holdingBattery);
		}

		private void OnInteracted()
		{
			_heavy.ConsumeHeld();
			_batteryVisual.SetActive(true);
			_facts.SetBatteryInserted(true);
		}
	}
}
