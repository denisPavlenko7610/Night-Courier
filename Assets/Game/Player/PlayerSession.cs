using System;
using NightCourier.App;
using UnityEngine;

namespace NightCourier.Player
{
	public sealed class PlayerSession : MonoBehaviour
	{
		[SerializeField] private PlayerMotor _motor;
		[SerializeField] private PlayerInteractor _interactor;

		private IDisposable _gameplayScope;

		public PlayerInteractor Interactor => _interactor;

		public void Begin(GameServices services)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			_gameplayScope?.Dispose();

			_motor.Bind(services.Input.Actions, services.Settings);
			_interactor.Bind(services.Input.Actions);
			_gameplayScope = services.Input.PushGameplay();
		}

		private void OnDestroy()
		{
			_gameplayScope?.Dispose();
			_gameplayScope = null;
		}
	}
}
