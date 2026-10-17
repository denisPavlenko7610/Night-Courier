using System;
using NightCourier.Contracts;
using NightCourier.Packages;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class WarehouseLighting : MonoBehaviour, ILightField
	{
		[SerializeField] private Light[] _lights = Array.Empty<Light>();

		private WarehouseFacts _facts;
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
			foreach (Light light in _lights)
			{
				if (!light.enabled || !light.gameObject.activeInHierarchy || light.intensity <= 0f)
				{
					continue;
				}

				if (light.type == LightType.Directional || (light.transform.position - point).sqrMagnitude <= light.range * light.range)
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
