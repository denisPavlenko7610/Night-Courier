using System;
using NightCourier.Contracts;
using NightCourier.Packages;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class WarehouseLighting : MonoBehaviour, ILightField
	{
		[SerializeField] private Light[] _lights = Array.Empty<Light>();
		[SerializeField] private LayerMask _occluders = 1;

		private WarehouseFacts _facts;
		private Func<Vector3, Vector3, bool> _isBlocked;
		private bool _blackout;

		public void Bind(LocationState state, WarehouseFacts facts)
		{
			Unbind();

			_facts = facts ?? throw new ArgumentNullException(nameof(facts));
			_blackout = state == LocationState.Blackout;
			_facts.Changed += Apply;

			Apply();
		}

		public bool IsLit(Vector3 point)
		{
			_isBlocked ??= IsBlocked;

			foreach (Light light in _lights)
			{
				if (!light.enabled || !light.gameObject.activeInHierarchy || light.intensity <= 0f)
				{
					continue;
				}

				Transform lightTransform = light.transform;
				LightSource source = new(
					light.type,
					lightTransform.position,
					lightTransform.forward,
					light.range,
					light.spotAngle,
					light.shadows != LightShadows.None);

				if (LightReach.Reaches(source, point, _isBlocked))
				{
					return true;
				}
			}

			return false;
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_facts != null)
			{
				_facts.Changed -= Apply;
				_facts = null;
			}
		}

		private bool IsBlocked(Vector3 from, Vector3 to)
		{
			return Physics.Linecast(from, to, _occluders, QueryTriggerInteraction.Ignore);
		}

		private void Apply()
		{
			bool powered = !_blackout || _facts.PowerOn;

			foreach (Light light in _lights)
			{
				light.enabled = powered;
			}
		}
	}
}
