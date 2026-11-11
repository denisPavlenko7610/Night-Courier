using System;
using NightCourier.Player;
using RD.Localization;
using RD.Tween;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class PlayerVitalsView : MonoBehaviour
	{
		private const float FadeSeconds = 0.4f;
		private const float FlashSeconds = 0.45f;
		private const float FlashBase = 0.35f;
		private const float FlashFullDamage = 40f;
		private const float LowHealthFraction = 0.35f;
		private const float LowPulseSpeed = 4f;
		private const float LowPulseMin = 0.15f;
		private const float LowPulseMax = 0.45f;
		private const string LowFillClass = "nc-hud__health-fill--low";
		private const string DangerTextClass = "nc-hud__text--danger";
		private const string HealthKey = "hud.health";
		private const string RecoveryKey = "hud.recovery";
		private const string HudGroupName = "hud-group";
		private const string HealthName = "health-label";
		private const string FillName = "health-fill";
		private const string VignetteName = "damage-vignette";
		private const string OverlayName = "death-overlay";
		private const string MessageName = "death-message";

		[SerializeField] private UIDocument _document;

		private ILocalizationService _localization;
		private PlayerVitals _vitals;
		private VisualElement _group;
		private Label _health;
		private VisualElement _fill;
		private VisualElement _vignette;
		private VisualElement _overlay;
		private Label _message;
		private float _overlayAlpha;
		private float _flash;
		private bool _low;

		public bool IsHudHidden { get; private set; }

		public float OverlayAlpha => _overlayAlpha;

		public void SetHudHidden(bool hidden)
		{
			IsHudHidden = hidden;

			if (_group != null)
			{
				UiQuery.SetVisible(_group, !hidden);
			}
		}

		public void HideRecoveryOverlay()
		{
			if (_overlay != null)
			{
				Tween.Kill(_overlay);
				SetOverlayAlpha(0f);
			}
		}

		public void Bind(ILocalizationService localization, PlayerVitals vitals)
		{
			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			Unbind();

			VisualElement root = _document.rootVisualElement;

			_localization = localization;
			_vitals = vitals != null ? vitals : throw new ArgumentNullException(nameof(vitals));
			_group = UiQuery.Require<VisualElement>(root, HudGroupName);
			_health = UiQuery.Require<Label>(root, HealthName);
			_fill = UiQuery.Require<VisualElement>(root, FillName);
			_vignette = UiQuery.Require<VisualElement>(root, VignetteName);
			_overlay = UiQuery.Require<VisualElement>(root, OverlayName);
			_message = UiQuery.Require<Label>(root, MessageName);

			UiQuery.SetVisible(_group, !IsHudHidden);
			SetOverlayAlpha(0f);

			_localization.LocaleChanged += OnLocaleChanged;
			_vitals.Changed += Refresh;
			_vitals.Damaged += OnDamaged;
			_vitals.Died += OnDied;
			_vitals.Respawned += OnRespawned;

			Refresh();
		}

		private void OnDestroy()
		{
			Unbind();

			if (_overlay != null)
			{
				Tween.Kill(_overlay);
			}

			if (_vignette != null)
			{
				Tween.Kill(_vignette);
			}
		}

		private void Update()
		{
			if (_low && _vitals != null)
			{
				ApplyVignette();
			}
		}

		private void Unbind()
		{
			if (_vitals == null)
			{
				return;
			}

			_localization.LocaleChanged -= OnLocaleChanged;
			_vitals.Changed -= Refresh;
			_vitals.Damaged -= OnDamaged;
			_vitals.Died -= OnDied;
			_vitals.Respawned -= OnRespawned;
			_vitals = null;
		}

		private void OnLocaleChanged(LocaleChanged change)
		{
			Refresh();
		}

		private void Refresh()
		{
			float fraction = Mathf.Clamp01(_vitals.Fraction);

			_low = fraction <= LowHealthFraction && _vitals.IsAlive;
			_health.text = _localization.Get(HealthKey, _vitals.Current);
			_message.text = _localization.Get(RecoveryKey);
			_fill.style.width = new Length(fraction * 100f, LengthUnit.Percent);
			_fill.EnableInClassList(LowFillClass, fraction <= LowHealthFraction);
			_health.EnableInClassList(DangerTextClass, fraction <= LowHealthFraction);
			ApplyVignette();
		}

		private void OnDamaged(float amount)
		{
			Tween.Kill(_vignette);
			SetFlash(Mathf.Clamp01(FlashBase + amount / FlashFullDamage * (1f - FlashBase)));
			Tween.To(_vignette, () => _flash, SetFlash, 0f, FlashSeconds);
		}

		private void SetFlash(float value)
		{
			_flash = value;
			ApplyVignette();
		}

		private void ApplyVignette()
		{
			float low = 0f;

			if (_low)
			{
				float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * LowPulseSpeed);

				low = Mathf.Lerp(LowPulseMin, LowPulseMax, pulse);
			}

			_vignette.style.opacity = Mathf.Max(_flash, low);
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
			Tween.To(_overlay, () => _overlayAlpha, SetOverlayAlpha, alpha, FadeSeconds);
		}

		private void SetOverlayAlpha(float value)
		{
			_overlayAlpha = value;
			_overlay.style.opacity = value;
		}
	}
}
