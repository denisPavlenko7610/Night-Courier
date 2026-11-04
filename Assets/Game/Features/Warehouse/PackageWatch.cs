using System;
using System.Linq;
using NightCourier.Audio;
using NightCourier.Contracts;
using NightCourier.Packages;
using NightCourier.Player;
using RD.Localization;
using UnityEngine;

namespace NightCourier.UI
{
	public sealed class PackageWatch : IDisposable
	{
		private const float DamageNoticeCooldownSeconds = 1.5f;
		private const float DarkNoticeCooldownSeconds = 3f;
		private const string RuleViolatedKey = "notice.rule_violated";
		private const string DarkStartedKey = "notice.dark_started";
		private const string DarkClearedKey = "notice.dark_cleared";
		private const string PackageDestroyedReasonKey = "result.package_destroyed";

		private readonly ContractRun _run;
		private readonly CargoPackage _package;
		private readonly PackageHudView _hud;
		private readonly WarehouseAudio _sounds;
		private readonly ILocalizationService _localization;
		private readonly Action<string> _fail;
		private readonly KeepInLightRule _darkRule;

		private float _lastDamageNoticeAt = float.NegativeInfinity;
		private float _lastDarkNoticeAt = float.NegativeInfinity;

		public PackageWatch(
			ContractRun run,
			CargoPackage package,
			PackageHudView hud,
			WarehouseAudio sounds,
			ILocalizationService localization,
			Action<string> fail
		)
		{
			_run = run ?? throw new ArgumentNullException(nameof(run));
			_package = package != null ? package : throw new ArgumentNullException(nameof(package));
			_hud = hud != null ? hud : throw new ArgumentNullException(nameof(hud));
			_sounds = sounds != null ? sounds : throw new ArgumentNullException(nameof(sounds));
			_localization = localization ?? throw new ArgumentNullException(nameof(localization));
			_fail = fail ?? throw new ArgumentNullException(nameof(fail));

			_package.Rules.Violated += OnRuleViolated;
			_package.Integrity.Changed += OnIntegrityChanged;
			_package.Integrity.Damaged += OnDamaged;
			_package.Integrity.Destroyed += OnDestroyed;

			if (_run.Spec.PackageRules.Contains(PackageRuleKind.KeepInLight))
			{
				_darkRule = _package.GetComponent<KeepInLightRule>();
				_darkRule.DarknessStarted += OnDark;
				_darkRule.LightRestored += OnLit;
			}
		}

		public void Dispose()
		{
			if (_package != null)
			{
				_package.Rules.Violated -= OnRuleViolated;
				_package.Integrity.Changed -= OnIntegrityChanged;
				_package.Integrity.Damaged -= OnDamaged;
				_package.Integrity.Destroyed -= OnDestroyed;
			}

			if (_darkRule != null)
			{
				_darkRule.DarknessStarted -= OnDark;
				_darkRule.LightRestored -= OnLit;
			}
		}

		private bool InTransit => _run.Phase == ContractPhase.InTransit;

		private void OnRuleViolated(PackageRuleKind kind)
		{
			_sounds.PlayViolation();
			_hud.Notify(RuleViolatedKey, _localization.Get(PackageRuleKeys.NameKey(kind)));
			_run.RegisterViolation(kind);
		}

		private void OnIntegrityChanged()
		{
			_hud.ShowIntegrity(_package.Integrity.Fraction);
		}

		private void OnDamaged(string damageType, float fractionLost)
		{
			if (!InTransit || Time.unscaledTime - _lastDamageNoticeAt < DamageNoticeCooldownSeconds)
			{
				return;
			}

			_lastDamageNoticeAt = Time.unscaledTime;
			_hud.Notify(PackageDamageKeys.NoticeKey(damageType), Mathf.Max(1, Mathf.RoundToInt(fractionLost * 100f)));
		}

		private void OnDestroyed()
		{
			_fail(PackageDestroyedReasonKey);
		}

		private void OnDark()
		{
			if (!InTransit || Time.unscaledTime - _lastDarkNoticeAt < DarkNoticeCooldownSeconds)
			{
				return;
			}

			_lastDarkNoticeAt = Time.unscaledTime;
			_hud.Notify(DarkStartedKey, Mathf.RoundToInt(_darkRule.DarkLimitSeconds));
		}

		private void OnLit()
		{
			if (InTransit)
			{
				_hud.Notify(DarkClearedKey);
			}
		}
	}
}