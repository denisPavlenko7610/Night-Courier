using System;
using NightCourier.Player;
using RD.Mechanics.CarryObject;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class BatteryCell : MonoBehaviour
	{
		private const string CarriedLayerName = "Carried";

		[SerializeField] private Interactable _interactable;
		[SerializeField] private CarryObject _carry;

		private PlayerHeavyCarry _heavy;
		private int _restLayer;
		private int _carriedLayer;

		public void Bind(PlayerHeavyCarry heavy)
		{
			Unbind();

			_heavy = heavy != null ? heavy : throw new ArgumentNullException(nameof(heavy));
			_restLayer = gameObject.layer;
			_carriedLayer = LayerMask.NameToLayer(CarriedLayerName);
			_interactable.Interacted += OnInteracted;
			_heavy.HeldChanged += Refresh;

			Refresh();
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

			if (_heavy != null)
			{
				_heavy.HeldChanged -= Refresh;
				_heavy = null;
			}
		}

		private void OnInteracted()
		{
			_heavy.TryGrab();
		}

		private void Refresh()
		{
			_interactable.SetAvailable(!_carry.IsCarried);
			SetLayer(_carry.IsCarried ? _carriedLayer : _restLayer);
		}

		private void SetLayer(int layer)
		{
			foreach (Transform child in GetComponentsInChildren<Transform>(true))
			{
				child.gameObject.layer = layer;
			}
		}
	}
}
