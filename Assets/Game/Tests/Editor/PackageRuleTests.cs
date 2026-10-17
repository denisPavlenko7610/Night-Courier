#if UNITY_INCLUDE_TESTS
using System;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Packages;
using NUnit.Framework;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class PackageRuleTests
	{
		private sealed class FixedLight : ILightField
		{
			public bool Lit { get; set; }

			public bool IsLit(Vector3 point)
			{
				return Lit;
			}
		}

		private GameObject _package;

		[TearDown]
		public void TearDown()
		{
			if (_package != null)
			{
				UnityEngine.Object.DestroyImmediate(_package);
			}
		}

		private static ContractRun StartRun(double timeLimit)
		{
			ContractBook book = new(new[] { ContractFixtures.Spec("only", timeLimit: timeLimit) });
			CampaignState campaign = CampaignState.CreateNew(book);
			ContractRun run = campaign.Contracts.Accept("only");

			run.CollectPackage();
			run.Depart();

			return run;
		}

		[Test]
		public void CalculateDamage_IsZeroBelowThresholdAndScalesAbove()
		{
			Assert.That(PackageImpact.CalculateDamage(3f, 4f, 10f), Is.EqualTo(0d));
			Assert.That(PackageImpact.CalculateDamage(4f, 4f, 10f), Is.EqualTo(0d));
			Assert.That(PackageImpact.CalculateDamage(6f, 4f, 10f), Is.EqualTo(20d).Within(0.001));
		}

		[Test]
		public void DoNotDrop_ReleaseOnUnsafeSurfaceIsViolation()
		{
			Assert.That(DoNotDropRule.IsViolation(new PackageImpactInfo(1f, false, true), 8f), Is.True);
		}

		[Test]
		public void DoNotDrop_ReleaseOnSafeSurfaceIsAllowed()
		{
			Assert.That(DoNotDropRule.IsViolation(new PackageImpactInfo(6f, true, true), 8f), Is.False);
		}

		[Test]
		public void DoNotDrop_HardImpactWithoutReleaseIsViolation()
		{
			Assert.That(DoNotDropRule.IsViolation(new PackageImpactInfo(9f, false, false), 8f), Is.True);
			Assert.That(DoNotDropRule.IsViolation(new PackageImpactInfo(2f, false, false), 8f), Is.False);
		}

		[Test]
		public void KeepInLight_ViolatesEveryTenDarkSecondsAndResetsWhenLit()
		{
			_package = new GameObject("Package");
			KeepInLightRule rule = _package.AddComponent<KeepInLightRule>();
			rule.Activate(new PackageRuleContext(null, new FixedLight()));

			int violations = 0;
			rule.Violated += () => violations++;

			rule.Advance(9.5d, false);

			Assert.That(violations, Is.EqualTo(0));

			rule.Advance(0.5d, false);

			Assert.That(violations, Is.EqualTo(1));

			rule.Advance(9d, false);
			rule.Advance(5d, true);
			rule.Advance(9d, false);

			Assert.That(violations, Is.EqualTo(1));
			Assert.That(rule.DarkSeconds, Is.EqualTo(9d).Within(0.001));

			rule.Advance(1d, false);

			Assert.That(violations, Is.EqualTo(2));
		}

		[Test]
		public void KeepInLight_AdvanceBeforeActivation_Throws()
		{
			_package = new GameObject("Package");
			KeepInLightRule rule = _package.AddComponent<KeepInLightRule>();

			Assert.Throws<InvalidOperationException>(() => rule.Advance(1d, false));
		}

		[Test]
		public void DeliveryDeadline_ViolatesWhenRunDeadlineExpires()
		{
			ContractRun run = StartRun(30d);
			_package = new GameObject("Package");
			DeliveryDeadlineRule rule = _package.AddComponent<DeliveryDeadlineRule>();
			rule.Activate(new PackageRuleContext(run, null));

			int violations = 0;
			rule.Violated += () => violations++;

			run.Tick(29d);

			Assert.That(violations, Is.EqualTo(0));

			run.Tick(2d);

			Assert.That(violations, Is.EqualTo(1));

			run.Tick(10d);

			Assert.That(violations, Is.EqualTo(1));
		}

		[Test]
		public void RuleSet_ActivatesOnlyRequestedRulesAndRelaysKind()
		{
			ContractRun run = StartRun(30d);
			_package = new GameObject("Package");
			PackageRuleSet set = _package.AddComponent<PackageRuleSet>();
			KeepInLightRule light = _package.AddComponent<KeepInLightRule>();
			DeliveryDeadlineRule deadline = _package.AddComponent<DeliveryDeadlineRule>();
			light.enabled = false;
			deadline.enabled = false;

			PackageRuleKind? reported = null;
			set.Violated += kind => reported = kind;

			set.Configure(new[] { PackageRuleKind.DeliveryDeadline }, new PackageRuleContext(run, null));
			run.Tick(31d);

			Assert.That(set.Active, Is.EqualTo(new[] { PackageRuleKind.DeliveryDeadline }));
			Assert.That(deadline.enabled, Is.True);
			Assert.That(light.enabled, Is.False);
			Assert.That(reported, Is.EqualTo(PackageRuleKind.DeliveryDeadline));
		}

		[Test]
		public void RuleSet_MissingComponent_Throws()
		{
			_package = new GameObject("Package");
			PackageRuleSet set = _package.AddComponent<PackageRuleSet>();

			Assert.Throws<InvalidOperationException>(
				() => set.Configure(new[] { PackageRuleKind.KeepInLight }, new PackageRuleContext(null, new FixedLight())));
		}

		[Test]
		public void RuleKeys_CoverEveryRule()
		{
			foreach (PackageRuleKind kind in Enum.GetValues(typeof(PackageRuleKind)))
			{
				Assert.That(PackageRuleKeys.NameKey(kind), Does.StartWith("rule."));
			}
		}

		[Test]
		public void Spec_RejectsDuplicateRulesAndDeadlineWithoutTimeLimit()
		{
			Assert.Throws<ArgumentException>(() => new ContractSpec(
				"x", "x.t", "x.d", ContractType.Deliver, "warehouse", LocationState.Normal, ContractRisk.Low, 100, 0d,
				Array.Empty<string>(), false, Array.Empty<ContractObjectiveSpec>(),
				new[] { PackageRuleKind.DoNotDrop, PackageRuleKind.DoNotDrop }));

			Assert.Throws<ArgumentException>(() => new ContractSpec(
				"x", "x.t", "x.d", ContractType.Deliver, "warehouse", LocationState.Normal, ContractRisk.Low, 100, 0d,
				Array.Empty<string>(), false, Array.Empty<ContractObjectiveSpec>(),
				new[] { PackageRuleKind.DeliveryDeadline }));

			ContractSpec valid = new(
				"x", "x.t", "x.d", ContractType.Deliver, "warehouse", LocationState.Normal, ContractRisk.Low, 100, 60d,
				Array.Empty<string>(), false, Array.Empty<ContractObjectiveSpec>(),
				new[] { PackageRuleKind.DeliveryDeadline, PackageRuleKind.DoNotDrop });

			Assert.That(valid.PackageRules, Is.EqualTo(new[] { PackageRuleKind.DeliveryDeadline, PackageRuleKind.DoNotDrop }));
		}
	}
}
#endif
