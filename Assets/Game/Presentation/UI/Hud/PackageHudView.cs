using System;
using System.Collections.Generic;
using NightCourier.Contracts;
using NightCourier.Controls;
using NightCourier.Packages;
using RD.Localization;
using RD.Notifications;
using RD.Timers;
using UnityEngine;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public sealed class PackageHudView : MonoBehaviour
	{
		private const float NoticeSeconds = 3f;
		private const float AnnounceSeconds = 7f;
		private const int MaxNoticeLines = 3;
		private const string RulesKey = "hud.package_rules";
		private const string IntegrityKey = "hud.package_integrity";
		private const string DarkTimerKey = "hud.dark_timer";
		private const string CarryKey = "hud.carry_hint";
		private const int CriticalDarkSeconds = 3;
		private const float CriticalPulseSpeed = 9f;
		private const float CriticalPulseMin = 0.55f;
		private const string CriticalClass = "nc-hud__dark-timer--critical";
		private const string CarryName = "carry-hint";
		private const string RuleSeparator = ", ";
		private const string RulesName = "package-rules";
		private const string IntegrityName = "package-integrity";
		private const string NoticeName = "notifications";
		private const string DarkTimerName = "dark-timer";

		[SerializeField] private UIDocument _document;
		[SerializeField] private NotificationAssets _notificationAssets;

		private NotificationCenter _notifications;
		private NotificationView _notificationView;

		private ILocalizationService _localization;
		private Label _rules;
		private Label _integrity;
		private Label _darkTimer;
		private Label _carry;
		private GameInput _input;
		private bool _carrying;
		private KeepInLightRule _darkRule;
		private bool _inDarkness;
		private int _darkSecondsShown = -1;

		public string DarkTimerText => _darkTimer?.text;

		public bool DarkTimerVisible => _darkTimer != null && _darkTimer.style.display != DisplayStyle.None;

		public string NoticeText => _notifications == null ? string.Empty :
			string.Join("\n", System.Linq.Enumerable.Select(_notifications.Visible, notice => notice.Message));

		public bool NoticeVisible => _notifications != null && _notifications.Visible.Count > 0;

		public void Bind(ILocalizationService localization, IReadOnlyList<PackageRuleKind> rules, GameInput input)
		{
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));

			if (_input != null)
			{
				_input.SchemeChanged -= RefreshCarry;
			}

			_input = input ?? throw new ArgumentNullException(nameof(input));
			_input.SchemeChanged += RefreshCarry;

			VisualElement root = _document.rootVisualElement;

			_rules = UiQuery.Require<Label>(root, RulesName);
			_integrity = UiQuery.Require<Label>(root, IntegrityName);
			_notificationView?.Dispose();
			_notifications?.Dispose();
			_notifications = new NotificationCenter(new NotificationOptions(MaxNoticeLines, 0), UnityClocks.Scaled);
			_notificationView = new NotificationView(
				_notifications, UiQuery.Require<VisualElement>(root, NoticeName), _notificationAssets,
				NotificationAnchor.BottomCenter, NotificationProfile.Desktop);
			_darkTimer = UiQuery.Require<Label>(root, DarkTimerName);
			_carry = UiQuery.Require<Label>(root, CarryName);
			_carrying = false;
			_darkRule = null;
			_inDarkness = false;
			_darkSecondsShown = -1;
			UiQuery.SetVisible(_darkTimer, false);

			List<string> names = new(rules.Count);

			foreach (PackageRuleKind rule in rules)
			{
				names.Add(localization.Get(PackageRuleKeys.NameKey(rule)));
			}

			UiQuery.SetVisible(_rules, names.Count > 0);
			_rules.text = localization.Get(RulesKey, UiStyle.Tint(string.Join(RuleSeparator, names), UiStyle.AccentColor));

			ShowIntegrity(1f);
			RefreshCarry();
		}

		public void SetCarrying(bool carrying)
		{
			_carrying = carrying;
			RefreshCarry();
		}

		private void RefreshCarry()
		{
			if (_carry == null || _input == null)
			{
				return;
			}

			UiQuery.SetVisible(_carry, _carrying);

			if (_carrying)
			{
				string key = ControlLabels.PromptLabel(_input.Actions.Drop, _input.Scheme, _input.GamepadStyle);

				_carry.text = _localization.Get(CarryKey, key);
			}
		}

		public void SetDarkness(KeepInLightRule rule, bool active)
		{
			_darkRule = rule;
			_inDarkness = active;
			_darkSecondsShown = -1;
			UiQuery.SetVisible(_darkTimer, active);
			RefreshDarkTimer();
		}

		private void RefreshDarkTimer()
		{
			if (!_inDarkness || _darkRule == null)
			{
				return;
			}

			int seconds = Mathf.Max(0, Mathf.CeilToInt(_darkRule.DarkLimitSeconds - (float)_darkRule.DarkSeconds));

			if (seconds != _darkSecondsShown)
			{
				_darkSecondsShown = seconds;
				_darkTimer.text = _localization.Get(DarkTimerKey, seconds);
				_darkTimer.EnableInClassList(CriticalClass, seconds <= CriticalDarkSeconds);
			}

			_darkTimer.style.opacity = seconds <= CriticalDarkSeconds
				? Mathf.Lerp(CriticalPulseMin, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * CriticalPulseSpeed))
				: 1f;
		}

		public void ShowIntegrity(float fraction)
		{
			float clamped = Mathf.Clamp01(fraction);

			_integrity.text = _localization.Get(
				IntegrityKey,
				UiStyle.Tint(Mathf.RoundToInt(clamped * 100f) + "%", UiStyle.IntegrityColor(clamped)));
		}

		public void Notify(string key, params object[] arguments)
		{
			Show(_localization.Get(key, arguments), NoticeSeconds);
		}

		public void Announce(string key)
		{
			Show(_localization.Get(key), AnnounceSeconds);
		}

		private void Show(string text, float seconds)
		{
			if (_notifications.Visible.Count == MaxNoticeLines)
			{
				_notifications.Visible[0].Dismiss();
			}

			_notifications.Post(new NotificationRequest(text, duration: seconds));
		}

		private void Update()
		{
			RefreshDarkTimer();
		}

		private void OnDestroy()
		{
			if (_input != null)
			{
				_input.SchemeChanged -= RefreshCarry;
			}

			_notificationView?.Dispose();
			_notifications?.Dispose();
		}
	}
}
