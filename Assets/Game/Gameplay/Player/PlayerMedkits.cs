using System;
using NightCourier.Controls;
using NightCourier.Loadout;
using RD.Input;
using UnityEngine;

namespace NightCourier.Player
{
	public sealed class PlayerMedkits : MonoBehaviour
	{
		[SerializeField] private PlayerVitals _vitals;

		private IDisposable _subscription;
		private CourierLoadout _loadout;

		public int Count => _loadout?.MedkitsEquipped ?? 0;

		public event Action Changed;

		public event Action Used;

		public void Bind(PlayerInputActions actions)
		{
			if (actions == null)
			{
				throw new ArgumentNullException(nameof(actions));
			}

			_subscription?.Dispose();
			_subscription = actions.UseMedkit.SubscribePerformed(_ => TryUse());
		}

		public void SetLoadout(CourierLoadout loadout)
		{
			if (_loadout != null)
			{
				_loadout.Changed -= OnLoadoutChanged;
			}

			_loadout = loadout ?? throw new ArgumentNullException(nameof(loadout));
			_loadout.Changed += OnLoadoutChanged;

			Changed?.Invoke();
		}

		public bool TryUse()
		{
			if (_loadout == null || !_vitals.IsAlive || _vitals.IsFull || !_loadout.TryConsumeMedkit())
			{
				return false;
			}

			_vitals.Heal(LoadoutRules.MedkitHealAmount);
			Used?.Invoke();

			return true;
		}

		private void OnDestroy()
		{
			_subscription?.Dispose();

			if (_loadout != null)
			{
				_loadout.Changed -= OnLoadoutChanged;
			}
		}

		private void OnLoadoutChanged()
		{
			Changed?.Invoke();
		}
	}
}
