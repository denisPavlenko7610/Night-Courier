using System;
using NightCourier.Controls;
using RD.Input;
using RD.Mechanics.PickupDrop;
using UnityEngine;

namespace NightCourier.Player
{
	public sealed class PlayerCarry : MonoBehaviour
	{
		[SerializeField] private PickupInteractor _pickup;
		[SerializeField] private PlayerHeavyCarry _heavy;

		private IDisposable _dropSubscription;
		private PickupItem _held;

		public bool IsCarrying => _held != null;

		public PickupItem Held => _held;

		public event Action HeldChanged;

		public event Action PickUpRefused;

		public void Bind(PlayerInputActions actions)
		{
			if (actions == null)
			{
				throw new ArgumentNullException(nameof(actions));
			}

			_dropSubscription?.Dispose();
			_dropSubscription = actions.Drop.SubscribePerformed(_ => Drop());
		}

		public bool TryPickUp()
		{
			if (_held != null)
			{
				return false;
			}

			if (_heavy.IsCarrying)
			{
				PickUpRefused?.Invoke();
				return false;
			}

			if (!_pickup.TryPickUpNearest())
			{
				return false;
			}

			_held = _pickup.HeldItem;
			HeldChanged?.Invoke();

			return true;
		}

		public bool Drop()
		{
			if (_held == null || !_pickup.DropHeldItem())
			{
				return false;
			}

			_held = null;
			HeldChanged?.Invoke();

			return true;
		}

		public bool ForceDrop()
		{
			if (_held == null)
			{
				return false;
			}

			if (Drop())
			{
				return true;
			}

			_pickup.enabled = false;
			_pickup.enabled = true;
			_held = null;
			HeldChanged?.Invoke();

			return true;
		}

		public void ConsumeHeld()
		{
			PickupItem item = _held;

			if (item == null)
			{
				return;
			}

			_held = null;
			Destroy(item.gameObject);
			HeldChanged?.Invoke();
		}

		private void OnDestroy()
		{
			_dropSubscription?.Dispose();
		}
	}
}
