using System;
using NightCourier.Player;
using RD.Health;
using RD.Mechanics.BreakableObject;
using RD.Mechanics.Combat;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class BreakableBarricade : MonoBehaviour
	{
		private const string ForceSourceId = "player.force";

		[SerializeField] private Interactable _interactable;
		[SerializeField] private HealthCarrier _health;
		[SerializeField] private BreakableObject _breakable;
		[SerializeField, Min(1f)] private float _forceDamage = 100f;

		private Collider _traveller;

		public event Action Broken;

		public void Bind(Collider traveller)
		{
			Unbind();

			_traveller = traveller != null ? traveller : throw new ArgumentNullException(nameof(traveller));

			_interactable.Interacted += OnInteracted;
			_breakable.Broken += OnBroken;
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

			if (_breakable != null)
			{
				_breakable.Broken -= OnBroken;
			}
		}

		private void OnInteracted()
		{
			_health.ReceiveDamage(new DamageRequest(_forceDamage, default, ForceSourceId));
		}

		private void OnBroken()
		{
			_interactable.SetAvailable(false);

			foreach (Collider debris in GetComponentsInChildren<Collider>(true))
			{
				Physics.IgnoreCollision(debris, _traveller);
			}

			Broken?.Invoke();
		}
	}
}
