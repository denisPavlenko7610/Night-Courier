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
		private const string HealthKey = "hud.health";
		private const string RecoveryKey = "hud.recovery";
		private const string HudGroupName = "hud-group";
		private const string HealthName = "health-label";
		private const string OverlayName = "death-overlay";
		private const string MessageName = "death-message";

		[SerializeField] private UIDocument _document;

		private ILocalizationService _localization;
		private PlayerVitals _vitals;
		private VisualElement _group;
		private Label _health;
		private VisualElement _overlay;
		private Label _message;
		private float _overlayAlpha;

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
			_overlay = UiQuery.Require<VisualElement>(root, OverlayName);
			_message = UiQuery.Require<Label>(root, MessageName);

			UiQuery.SetVisible(_group, !IsHudHidden);
			SetOverlayAlpha(0f);

			_localization.LocaleChanged += OnLocaleChanged;
			_vitals.Changed += Refresh;
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
		}

		private void Unbind()
		{
			if (_vitals == null)
			{
				return;
			}

			_localization.LocaleChanged -= OnLocaleChanged;
			_vitals.Changed -= Refresh;
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
			_health.text = _localization.Get(HealthKey, _vitals.Current);
			_message.text = _localization.Get(RecoveryKey);
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
