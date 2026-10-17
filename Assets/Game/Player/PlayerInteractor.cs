using System;
using NightCourier.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCourier.Player
{
	public sealed class PlayerInteractor : MonoBehaviour
	{
		[SerializeField] private Transform _origin;
		[SerializeField, Min(0.5f)] private float _reach = 2.5f;
		[SerializeField] private LayerMask _mask = ~0;

		private InputAction _interact;
		private Collider _lastCollider;
		private Interactable _lastFound;
		private Interactable _target;
		private Interactable _active;

		public Interactable Target => _target;

		public float HoldProgress => _active != null ? _active.HoldProgress : 0f;

		public event Action TargetChanged;

		public void Bind(PlayerInputActions actions)
		{
			if (actions == null)
			{
				throw new ArgumentNullException(nameof(actions));
			}

			_interact = actions.Interact;
		}

		private void Update()
		{
			if (_interact == null)
			{
				return;
			}

			if (!_interact.enabled)
			{
				ReleaseActive();
				SetTarget(null);
				return;
			}

			Interactable candidate = Find();

			if (_active != null && candidate != _active)
			{
				ReleaseActive();
			}

			SetTarget(candidate);

			if (_target == null)
			{
				return;
			}

			if (_interact.WasPressedThisFrame())
			{
				_active = _target;
				_target.Press();
			}

			if (_active == null)
			{
				return;
			}

			if (_interact.IsPressed())
			{
				_active.Hold(Time.deltaTime);
			}
			else
			{
				ReleaseActive();
			}
		}

		private void OnDisable()
		{
			ReleaseActive();
		}

		private Interactable Find()
		{
			if (!Physics.Raycast(_origin.position, _origin.forward, out RaycastHit hit, _reach, _mask, QueryTriggerInteraction.Ignore))
			{
				_lastCollider = null;
				_lastFound = null;
				return null;
			}

			if (hit.collider != _lastCollider)
			{
				_lastCollider = hit.collider;
				_lastFound = hit.collider.GetComponentInParent<Interactable>();
			}

			return _lastFound != null && _lastFound.IsAvailable ? _lastFound : null;
		}

		private void ReleaseActive()
		{
			if (_active == null)
			{
				return;
			}

			Interactable released = _active;
			_active = null;

			if (released != null)
			{
				released.Release();
			}
		}

		private void SetTarget(Interactable target)
		{
			if (_target == target)
			{
				return;
			}

			_target = target;
			TargetChanged?.Invoke();
		}
	}
}
