using UnityEngine;

namespace NightCourier.Packages
{
	public sealed class PackageUnstableEffect : MonoBehaviour
	{
		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		[SerializeField] private PackageImpact _impact;
		[SerializeField] private Renderer _renderer;
		[SerializeField, Min(1f)] private float _damageMultiplier = 2f;
		[SerializeField, Min(0.1f)] private float _pulsesPerSecond = 2f;
		[SerializeField] private Color _warningColor = new(1f, 0.2f, 0.1f);

		private MaterialPropertyBlock _block;
		private Color _calmColor;

		public bool IsActive => enabled;

		public void Activate()
		{
			if (enabled)
			{
				return;
			}

			_block = new MaterialPropertyBlock();
			_calmColor = _renderer.sharedMaterial.GetColor(BaseColorId);
			_impact.DamageMultiplier = _damageMultiplier;
			enabled = true;
		}

		private void Update()
		{
			float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * _pulsesPerSecond * Mathf.PI * 2f);

			_block.SetColor(BaseColorId, Color.Lerp(_calmColor, _warningColor, wave));
			_renderer.SetPropertyBlock(_block);
		}
	}
}
