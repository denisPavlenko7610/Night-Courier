using System;
using NightCourier.Controls;
using RD.Input;
using RD.Mechanics.CarryObject;
using UnityEngine;

namespace NightCourier.Player
{
	public sealed class PlayerHeavyCarry : MonoBehaviour
	{
		[SerializeField] private CarryCarrier _carrier;
		[SerializeField] private PlayerCarry _packages;

		private IDisposable _dropSubscription;
		private bool _wasCarrying;

		public bool IsCarrying => _carrier.IsCarrying;

		public CarryObject Carried => _carrier.CarriedObject;

		public event Action HeldChanged;

		public event Action GrabRefused;

		public void Bind(PlayerInputActions actions)
		{
			if (actions == null)
			{
				throw new ArgumentNullException(nameof(actions));
			}

			_dropSubscription?.Dispose();
			_dropSubscription = actions.Drop.SubscribePerformed(_ => Release());
		}

		public bool TryGrab()
		{
			if (_carrier.IsCarrying)
			{
				return false;
			}

			if (_packages.IsCarrying)
			{
				GrabRefused?.Invoke();
				return false;
			}

			bool grabbed = _carrier.TryGrabNearest();

			Sync();

			return grabbed;
		}

		public bool Release()
		{
			bool released = _carrier.Release();

			Sync();

			return released;
		}

		public void ConsumeHeld()
		{
			CarryObject carried = _carrier.CarriedObject;

			if (carried == null)
			{
				return;
			}

			_carrier.Release();
			Destroy(carried.gameObject);
			Sync();
		}

		private void Update()
		{
			Sync();
		}

		private void OnDestroy()
		{
			_dropSubscription?.Dispose();
		}

		private void Sync()
		{
			if (_wasCarrying == _carrier.IsCarrying)
			{
				return;
			}

			_wasCarrying = _carrier.IsCarrying;
			HeldChanged?.Invoke();
		}
	}
}
