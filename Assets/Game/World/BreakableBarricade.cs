using System;
using RD.Mechanics.BreakableObject;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class BreakableBarricade : MonoBehaviour
	{
		[SerializeField] private BreakableObject _breakable;

		private Collider _traveller;

		public event Action Broken;

		public void Bind(Collider traveller)
		{
			Unbind();

			_traveller = traveller != null ? traveller : throw new ArgumentNullException(nameof(traveller));

			_breakable.Broken += OnBroken;
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
			foreach (Collider debris in GetComponentsInChildren<Collider>(true))
			{
				Physics.IgnoreCollision(debris, _traveller);
			}

			Broken?.Invoke();
		}
	}
}
