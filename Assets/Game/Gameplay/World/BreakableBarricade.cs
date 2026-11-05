using System;
using RD.Mechanics.BreakableObject;
using RD.Mechanics.Interactable;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class BreakableBarricade : MonoBehaviour
	{
		[SerializeField] private BreakableObject _breakable;
		[SerializeField] private Interactable _notice;

		private Collider _traveller;

		public event Action Broken;

		public void Bind(Collider traveller)
		{
			Unbind();

			_traveller = traveller != null ? traveller : throw new ArgumentNullException(nameof(traveller));

			_breakable.Broken += OnBroken;

			_notice.SetAvailable(false, BlockedReasons.BarricadeIntact);
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_breakable != null)
			{
				_breakable.Broken -= OnBroken;
			}
		}

		private void OnBroken()
		{
			_notice.SetAvailable(false);

			foreach (Collider debris in GetComponentsInChildren<Collider>(true))
			{
				Physics.IgnoreCollision(debris, _traveller);
			}

			Broken?.Invoke();
		}
	}
}
