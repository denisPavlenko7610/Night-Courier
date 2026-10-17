using System;
using RD.Mechanics.HoldInteraction;
using UnityEngine;

namespace NightCourier.Player
{
	public sealed class Interactable : MonoBehaviour
	{
		[SerializeField] private string _promptKey;
		[SerializeField] private HoldInteraction _hold;

		private bool _subscribed;

		public string PromptKey => _promptKey;

		public bool IsAvailable { get; private set; } = true;

		public bool RequiresHold => _hold != null;

		public float HoldProgress => _hold != null ? _hold.Progress : 0f;

		public event Action Interacted;

		public void SetAvailable(bool available)
		{
			IsAvailable = available;

			if (!available)
			{
				Release();
			}
		}

		public void Press()
		{
			if (!IsAvailable)
			{
				return;
			}

			if (_hold == null)
			{
				Interacted?.Invoke();
				return;
			}

			EnsureSubscribed();
			_hold.TryBeginHold();
		}

		public void Hold(float deltaSeconds)
		{
			if (_hold != null && IsAvailable)
			{
				_hold.Tick(deltaSeconds);
			}
		}

		public void Release()
		{
			if (_hold != null)
			{
				_hold.Cancel();
			}
		}

		private void OnDestroy()
		{
			if (_subscribed && _hold != null)
			{
				_hold.Completed -= OnHoldCompleted;
			}
		}

		private void EnsureSubscribed()
		{
			if (_subscribed)
			{
				return;
			}

			_hold.Completed += OnHoldCompleted;
			_subscribed = true;
		}

		private void OnHoldCompleted()
		{
			_hold.ResetInteraction();
			Interacted?.Invoke();
		}
	}
}
