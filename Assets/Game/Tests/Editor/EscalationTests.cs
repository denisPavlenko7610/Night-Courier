#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using NightCourier.App;
using NightCourier.Contracts;
using NUnit.Framework;

namespace NightCourier.Tests
{
	public sealed class EscalationTests
	{
		private static ContractSpec SpecWith(params EscalationStep[] steps)
		{
			return new ContractSpec(
				"x", "x.t", "x.d", ContractType.Deliver, "warehouse", LocationState.Normal, ContractRisk.Low, 100, 0d,
				Array.Empty<string>(), false, Array.Empty<ContractObjectiveSpec>(), Array.Empty<PackageRuleKind>(), steps);
		}

		private static ContractRun StartRun(ContractSpec spec)
		{
			CampaignState campaign = CampaignState.CreateNew(new ContractBook(new[] { spec }));
			ContractRun run = campaign.Contracts.Accept(spec.Id);

			run.CollectPackage();
			run.Depart();

			return run;
		}

		[Test]
		public void Step_BelowOne_Throws()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new EscalationStep(0, EscalationEffect.FailContract));
		}

		[Test]
		public void Spec_SortsStepsAndReturnsEffectsAtExactCount()
		{
			ContractSpec spec = SpecWith(
				new EscalationStep(4, EscalationEffect.FailContract),
				new EscalationStep(1, EscalationEffect.PackageUnstable));

			Assert.That(spec.Escalation[0].AtViolation, Is.EqualTo(1));
			Assert.That(spec.GetEscalation(1), Is.EqualTo(new[] { EscalationEffect.PackageUnstable }));
			Assert.That(spec.GetEscalation(2), Is.Empty);
			Assert.That(spec.GetEscalation(4), Is.EqualTo(new[] { EscalationEffect.FailContract }));
		}

		[Test]
		public void Spec_RepeatedEffect_Throws()
		{
			Assert.Throws<ArgumentException>(() => SpecWith(
				new EscalationStep(1, EscalationEffect.FailContract),
				new EscalationStep(2, EscalationEffect.FailContract)));
		}

		[Test]
		public void Spec_SpawnEnemyStep_IsReportedAtItsViolation()
		{
			ContractSpec spec = SpecWith(
				new EscalationStep(1, EscalationEffect.PackageUnstable),
				new EscalationStep(2, EscalationEffect.SpawnEnemy),
				new EscalationStep(4, EscalationEffect.FailContract));

			Assert.That(spec.GetEscalation(2), Is.EqualTo(new[] { EscalationEffect.SpawnEnemy }));
			Assert.That(spec.GetEscalation(3), Is.Empty);
		}

		[Test]
		public void Spec_DefaultLadder_ReportsEachConsequenceAtItsViolation()
		{
			ContractSpec spec = SpecWith(
				new EscalationStep(1, EscalationEffect.PackageUnstable),
				new EscalationStep(2, EscalationEffect.SpawnEnemy),
				new EscalationStep(3, EscalationEffect.PackageHazardous),
				new EscalationStep(4, EscalationEffect.FailContract));

			Assert.That(spec.GetEscalation(1), Is.EqualTo(new[] { EscalationEffect.PackageUnstable }));
			Assert.That(spec.GetEscalation(2), Is.EqualTo(new[] { EscalationEffect.SpawnEnemy }));
			Assert.That(spec.GetEscalation(3), Is.EqualTo(new[] { EscalationEffect.PackageHazardous }));
			Assert.That(spec.GetEscalation(4), Is.EqualTo(new[] { EscalationEffect.FailContract }));
		}

		[Test]
		public void Spec_WithoutEscalation_HasNoSteps()
		{
			Assert.That(SpecWith().Escalation, Is.Empty);
			Assert.That(SpecWith().GetEscalation(1), Is.Empty);
		}

		[Test]
		public void Run_PackageViolationsRaiseEventWithRunningCount()
		{
			ContractRun run = StartRun(SpecWith());
			List<int> counts = new();
			run.PackageViolationRegistered += counts.Add;

			run.RegisterViolation();
			run.RegisterViolation();

			Assert.That(counts, Is.EqualTo(new[] { 1, 2 }));
			Assert.That(run.PackageViolationCount, Is.EqualTo(2));
			Assert.That(run.ViolationCount, Is.EqualTo(2));
		}

		[Test]
		public void Run_PropertyDamageCountsAsViolationButNotForTheLadder()
		{
			ContractRun run = StartRun(SpecWith());
			int raised = 0;
			run.PackageViolationRegistered += _ => raised++;

			run.RegisterPropertyDamage();

			Assert.That(raised, Is.EqualTo(0));
			Assert.That(run.PackageViolationCount, Is.EqualTo(0));
			Assert.That(run.ViolationCount, Is.EqualTo(1));
		}

		[Test]
		public void Run_DeathsAreCountedInTransitAndReportedInTheOutcome()
		{
			ContractSpec spec = SpecWith();
			CampaignState campaign = CampaignState.CreateNew(new ContractBook(new[] { spec }));
			ContractRun run = campaign.Contracts.Accept("x");

			run.RegisterDeath();

			Assert.That(run.DeathCount, Is.EqualTo(0));

			run.CollectPackage();
			run.Depart();
			run.RegisterDeath();
			run.RegisterDeath();

			Assert.That(run.DeathCount, Is.EqualTo(2));
			Assert.That(run.CaptureOutcome().DeathCount, Is.EqualTo(2));
			Assert.That(run.ViolationCount, Is.EqualTo(0));
		}

		[Test]
		public void Run_ViolationsOutsideTransit_AreIgnored()
		{
			CampaignState campaign = CampaignState.CreateNew(new ContractBook(new[] { SpecWith() }));
			ContractRun run = campaign.Contracts.Accept("x");
			int raised = 0;
			run.PackageViolationRegistered += _ => raised++;

			run.RegisterViolation();
			run.RegisterPropertyDamage();

			Assert.That(raised, Is.EqualTo(0));
			Assert.That(run.ViolationCount, Is.EqualTo(0));
		}
	}
}
#endif
