using System;
using NightCourier.App;
using NightCourier.Controls;
using NightCourier.Economy;
using NightCourier.Weapons;
using RD.Mechanics.DamageOverTime;
using UnityEngine;

namespace NightCourier.Player
{
	public sealed class PlayerSession : MonoBehaviour
	{
		[SerializeField] private PlayerMotor _motor;
		[SerializeField] private PlayerInteractor _interactor;
		[SerializeField] private PlayerCarry _carry;
		[SerializeField] private PlayerHeavyCarry _heavy;
		[SerializeField] private PlayerVitals _vitals;
		[SerializeField] private PlayerWeapons _weapons;
		[SerializeField] private PlayerMedkits _medkits;
		[SerializeField] private DamageOverTime _contamination;

		private GameInput _input;
		private IDisposable _gameplayScope;
		private IDisposable _deathScope;

		public PlayerInteractor Interactor => _interactor;

		public PlayerCarry Carry => _carry;

		public PlayerHeavyCarry Heavy => _heavy;

		public PlayerVitals Vitals => _vitals;

		public PlayerWeapons Weapons => _weapons;

		public PlayerMedkits Medkits => _medkits;

		public DamageOverTime Contamination => _contamination;

		public void Begin(GameServices services)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			_gameplayScope?.Dispose();
			_vitals.Died -= OnDied;
			_vitals.Respawned -= OnRespawned;

			_input = services.Input;

			_motor.Bind(services.Input.Actions, services.Settings);
			_interactor.Bind(services.Input.Actions);
			_carry.Bind(services.Input.Actions);
			_heavy.Bind(services.Input.Actions);
			_weapons.Bind(services.Input.Actions);
			_medkits.Bind(services.Input.Actions);
			_gameplayScope = services.Input.PushGameplay();

			_vitals.Died += OnDied;
			_vitals.Respawned += OnRespawned;

			if (services.Campaign.Current != null)
			{
				ApplyStats(services.Campaign.Current.Stats);
				_weapons.SetLoadout(services.Campaign.Current.Loadout);
				_medkits.SetLoadout(services.Campaign.Current.Loadout);
			}
		}

		public void ApplyStats(CourierStats stats)
		{
			if (stats == null)
			{
				throw new ArgumentNullException(nameof(stats));
			}

			_vitals.SetMaximumHealth(stats.MaxHealth);
			_motor.SetSpeedMultiplier(stats.MoveSpeedMultiplier);
			_weapons.SetReserveBonus(stats.ReserveAmmoBonus);
		}

		private void OnDestroy()
		{
			_vitals.Died -= OnDied;
			_vitals.Respawned -= OnRespawned;

			_deathScope?.Dispose();
			_deathScope = null;
			_gameplayScope?.Dispose();
			_gameplayScope = null;
		}

		private void OnDied()
		{
			_carry.ForceDrop();
			_heavy.Release();

			_deathScope?.Dispose();
			_deathScope = _input.PushLocked();
		}

		private void OnRespawned()
		{
			_deathScope?.Dispose();
			_deathScope = null;
		}
	}
}
