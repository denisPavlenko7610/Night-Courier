using System;
using NightCourier.App;
using RD.Foundation;
using RD.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCourier.Controls
{
	public sealed class GameInput : IDisposable
	{
		private readonly InputServices _services;

		private bool _disposed;
		private int _lockDepth;

		public GameInput(InputActionAsset asset)
		{
			if (asset == null)
			{
				throw new ArgumentNullException(nameof(asset));
			}

			_services = InputServices.CreateOwnedClone(asset, new PlayerPrefsInputBindingsStorage(GameSettings.InputBindingsKey));
			_services.Bindings.TryLoad();

			Actions = new PlayerInputActions(_services.Asset);

			_services.Contexts.CurrentMapsChanged += ApplyCursor;
			ApplyCursor();
		}

		public PlayerInputActions Actions { get; }

		public InputServices Services => _services;

		public IDisposable PushGameplay()
		{
			return _services.Contexts.Push(Actions.Gameplay);
		}

		public IDisposable PushLocked()
		{
			_lockDepth++;

			return new LockScope(this, _services.Contexts.Push(Array.Empty<InputActionMap>()));
		}

		public IDisposable PushMenu(Action onCancel = null)
		{
			IDisposable menu = _services.Contexts.Push(Actions.Ui);

			if (onCancel == null)
			{
				return menu;
			}

			IDisposable cancel = Actions.Cancel.SubscribePerformed(_ => onCancel());

			return new CompositeDisposable(new[] { menu, cancel });
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			_services.Contexts.CurrentMapsChanged -= ApplyCursor;
			_services.Dispose();

			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}

		private void ApplyCursor()
		{
			bool gameplay = false;

			foreach (InputActionMap map in _services.Contexts.CurrentMaps)
			{
				if (map == Actions.Gameplay)
				{
					gameplay = true;
					break;
				}
			}

			bool locked = gameplay || _lockDepth > 0;

			Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !locked;
		}

		private void Unlock()
		{
			_lockDepth--;
			ApplyCursor();
		}

		private sealed class LockScope : IDisposable
		{
			private readonly GameInput _owner;
			private readonly IDisposable _context;

			private bool _disposed;

			public LockScope(GameInput owner, IDisposable context)
			{
				_owner = owner;
				_context = context;
			}

			public void Dispose()
			{
				if (_disposed)
				{
					return;
				}

				_disposed = true;
				_context.Dispose();
				_owner.Unlock();
			}
		}
	}
}
