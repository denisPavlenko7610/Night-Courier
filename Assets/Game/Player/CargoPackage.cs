using System;
using NightCourier.Packages;
using RD.Mechanics.PickupDrop;
using UnityEngine;

namespace NightCourier.Player
{
	public sealed class CargoPackage : MonoBehaviour
	{
		[SerializeField] private PickupItem _item;
		[SerializeField] private Interactable _interactable;
		[SerializeField] private PackageImpact _impact;
		[SerializeField] private PackageIntegrity _integrity;
		[SerializeField] private PackageRuleSet _rules;
		[SerializeField] private PackageUnstableEffect _unstable;
		[SerializeField] private PackageHazardEffect _hazard;

		private PlayerCarry _carry;

		public PackageUnstableEffect Unstable => _unstable;

		public PackageHazardEffect Hazard => _hazard;

		public PackageImpact Impact => _impact;

		public PackageIntegrity Integrity => _integrity;

		public PackageRuleSet Rules => _rules;

		public event Action PickedUp;

		public void Bind(PlayerCarry carry)
		{
			Unbind();

			_carry = carry ?? throw new ArgumentNullException(nameof(carry));
			_interactable.Interacted += OnInteracted;
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
		}

		private void OnInteracted()
		{
			if (_carry.TryPickUp() && _carry.Held == _item)
			{
				PickedUp?.Invoke();
			}
		}
	}
}
