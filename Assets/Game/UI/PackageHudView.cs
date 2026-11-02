using System;
using System.Collections.Generic;
using NightCourier.Contracts;
using NightCourier.Packages;
using RD.Localization;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class PackageHudView : MonoBehaviour
	{
		private const float NoticeSeconds = 3f;
		private const float AnnounceSeconds = 7f;
		private const int MaxNoticeLines = 3;
		private const string RulesKey = "hud.package_rules";
		private const string IntegrityKey = "hud.package_integrity";
		private const string NoticeKey = "notice.text";
		private const string RuleSeparator = ", ";

		[SerializeField] private LocalizedLabel _rules;
		[SerializeField] private LocalizedLabel _integrity;
		[SerializeField] private LocalizedLabel _notice;

		private readonly List<string> _noticeLines = new(MaxNoticeLines);

		private ILocalizationService _localization;
		private float _noticeRemaining;

		public void Bind(ILocalizationService localization, IReadOnlyList<PackageRuleKind> rules)
		{
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));

			_rules.Bind(localization);
			_integrity.Bind(localization);
			_notice.Bind(localization);

			List<string> names = new(rules.Count);

			foreach (PackageRuleKind rule in rules)
			{
				names.Add(localization.Get(PackageRuleKeys.NameKey(rule)));
			}

			_rules.gameObject.SetActive(names.Count > 0);
			_rules.SetKey(RulesKey, string.Join(RuleSeparator, names));
			_notice.gameObject.SetActive(false);

			ShowIntegrity(1f);
		}

		public void ShowIntegrity(float fraction)
		{
			_integrity.SetKey(IntegrityKey, Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100f));
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
			if (_noticeRemaining <= 0f)
			{
				_noticeLines.Clear();
			}
			else if (_noticeLines.Count == MaxNoticeLines)
			{
				_noticeLines.RemoveAt(0);
			}

			_noticeLines.Add(text);
			_notice.gameObject.SetActive(true);
			_notice.SetKey(NoticeKey, string.Join("\n", _noticeLines));
			_noticeRemaining = Mathf.Max(_noticeRemaining, seconds);
		}

		private void Update()
		{
			if (_noticeRemaining <= 0f)
			{
				return;
			}

			_noticeRemaining -= Time.deltaTime;

			if (_noticeRemaining <= 0f)
			{
				_notice.gameObject.SetActive(false);
			}
		}
	}
}
