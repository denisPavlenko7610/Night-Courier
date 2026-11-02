#if UNITY_INCLUDE_TESTS
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Packages;
using NUnit.Framework;

namespace NightCourier.Tests
{
	public sealed class SettlementAdviceTests
	{
		private static ContractRun StartRun()
		{
			CampaignState campaign = CampaignState.CreateNew(new ContractBook(new[] { ContractFixtures.Spec("a") }));
			ContractRun run = campaign.Contracts.Accept("a");

			run.CollectPackage();
			run.Depart();

			return run;
		}

		[Test]
		public void CleanRun_GetsTheCleanLineOnly()
		{
			Assert.That(SettlementAdvice.For(new ContractOutcome(0, 10d, false)), Is.EqualTo(new[] { SettlementAdvice.CleanKey }));
		}

		[Test]
		public void BrokenRules_GetTheirOwnAdviceMostBrokenFirst()
		{
			ContractRun run = StartRun();

			run.RegisterViolation(PackageRuleKind.KeepInLight);
			run.RegisterViolation(PackageRuleKind.DoNotDrop);
			run.RegisterViolation(PackageRuleKind.DoNotDrop);

			ContractOutcome outcome = run.CaptureOutcome();

			Assert.That(outcome.PackageViolationCount, Is.EqualTo(3));
			Assert.That(outcome.RuleViolations[0].Kind, Is.EqualTo(PackageRuleKind.DoNotDrop));
			Assert.That(outcome.RuleViolations[0].Count, Is.EqualTo(2));
			Assert.That(
				SettlementAdvice.For(outcome),
				Is.EqualTo(new[]
				{
					PackageRuleKeys.AdviceKey(PackageRuleKind.DoNotDrop),
					PackageRuleKeys.AdviceKey(PackageRuleKind.KeepInLight)
				}));
		}

		[Test]
		public void Run_RecordsRuleViolationsOnlyInTransit()
		{
			CampaignState campaign = CampaignState.CreateNew(new ContractBook(new[] { ContractFixtures.Spec("a") }));
			ContractRun run = campaign.Contracts.Accept("a");

			run.RegisterViolation(PackageRuleKind.DoNotDrop);

			Assert.That(run.CaptureOutcome().RuleViolations, Is.Empty);
			Assert.That(run.ViolationCount, Is.EqualTo(0));
		}

		[Test]
		public void PropertyDamage_IsNotARuleViolation()
		{
			ContractRun run = StartRun();

			run.RegisterPropertyDamage();

			ContractOutcome outcome = run.CaptureOutcome();

			Assert.That(outcome.ViolationCount, Is.EqualTo(1));
			Assert.That(outcome.PackageViolationCount, Is.EqualTo(0));
			Assert.That(outcome.RuleViolations, Is.Empty);
			Assert.That(SettlementAdvice.For(outcome), Is.EqualTo(new[] { SettlementAdvice.PropertyKey }));
		}

		[Test]
		public void DamagedCargo_AndDeaths_AreExplained()
		{
			ContractOutcome outcome = new(0, 10d, false, deathCount: 1, cargoIntegrity: 0.6d);

			Assert.That(
				SettlementAdvice.For(outcome),
				Is.EqualTo(new[] { SettlementAdvice.CargoKey, SettlementAdvice.DeathKey }));
		}

		[Test]
		public void Overdue_ReplacesTheDeadlineRuleAdvice()
		{
			ContractOutcome outcome = new(
				1,
				400d,
				true,
				ruleViolations: new[] { new RuleViolationCount(PackageRuleKind.DeliveryDeadline, 1) });

			Assert.That(SettlementAdvice.For(outcome), Is.EqualTo(new[] { SettlementAdvice.OverdueKey }));
		}

		[Test]
		public void Advice_IsCappedAtThreeLines()
		{
			ContractOutcome outcome = new(
				3,
				400d,
				true,
				deathCount: 2,
				propertyDamageCount: 1,
				cargoIntegrity: 0.3d,
				ruleViolations: new[] { new RuleViolationCount(PackageRuleKind.DoNotDrop, 2) });

			Assert.That(SettlementAdvice.For(outcome).Count, Is.EqualTo(SettlementAdvice.MaxLines));
			Assert.That(SettlementAdvice.For(outcome)[0], Is.EqualTo(SettlementAdvice.OverdueKey));
		}

		[Test]
		public void CargoAdvice_AppearsOnlyBelowTheGradeAThreshold()
		{
			Assert.That(SettlementAdvice.For(new ContractOutcome(0, 1d, false, cargoIntegrity: 0.85d)), Does.Not.Contain(SettlementAdvice.CargoKey));
			Assert.That(SettlementAdvice.For(new ContractOutcome(0, 1d, false, cargoIntegrity: 0.84d)), Does.Contain(SettlementAdvice.CargoKey));
		}
	}
}
#endif
