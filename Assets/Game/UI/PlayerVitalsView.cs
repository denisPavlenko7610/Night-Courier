using System;
using NightCourier.Player;
using RD.Localization;
using RD.Tween;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class PlayerVitalsView : MonoBehaviour
	{
		private const float FadeSeconds = 0.4f;
		private const string HealthKey = "hud.health";

		[SerializeField] private LocalizedLabel _health;
		[SerializeField] private CanvasGroup _overlay;
		[SerializeField] private LocalizedLabel _overlayMessage;

		private PlayerVitals _vitals;

		public void Bind(ILocalizationService localization, PlayerVitals vitals)
		{
			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			Unbind();

			_vitals = vitals != null ? vitals : throw new ArgumentNullException(nameof(vitals));

			_health.Bind(localization);
			_overlayMessage.Bind(localization);
			_overlay.alpha = 0f;

			_vitals.Changed += Refresh;
			_vitals.Died += OnDied;
			_vitals.Respawned += OnRespawned;

			Refresh();
		}

		private void OnDestroy()
		{
			Unbind();
			Tween.Kill(_overlay);
		}

		private void Unbind()
		{
			if (_vitals == null)
			{
				return;
			}

			_vitals.Changed -= Refresh;
			_vitals.Died -= OnDied;
			_vitals.Respawned -= OnRespawned;
			_vitals = null;
		}

		private void Refresh()
		{
			_health.SetKey(HealthKey, _vitals.Current);
		}

		private void OnDied()
		{
			FadeTo(1f);
		}

		private void OnRespawned()
		{
			FadeTo(0f);
		}

		private void FadeTo(float alpha)
		{
			Tween.Kill(_overlay);
			Tween.To(_overlay, () => _overlay.alpha, value => _overlay.alpha = value, alpha, FadeSeconds);
		}
	}
}
