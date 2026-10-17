using System;
using NightCourier.App;
using NightCourier.Controls;
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

		private GameInput _input;
		private IDisposable _gameplayScope;
		private IDisposable _deathScope;

		public PlayerInteractor Interactor => _interactor;

		public PlayerCarry Carry => _carry;

		public PlayerHeavyCarry Heavy => _heavy;

		public PlayerVitals Vitals => _vitals;

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
			_gameplayScope = services.Input.PushGameplay();

			_vitals.Died += OnDied;
			_vitals.Respawned += OnRespawned;
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
