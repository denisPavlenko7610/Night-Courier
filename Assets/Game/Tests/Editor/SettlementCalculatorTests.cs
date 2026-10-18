#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NightCourier.Economy;
using NightCourier.Contracts;
using NUnit.Framework;

namespace NightCourier.Tests
{
	public sealed class SettlementCalculatorTests
	{
		[Test]
		public void BasePayOnly_WhenThereAreNoObjectives()
		{
			ContractSettlement settlement = SettlementCalculator.Calculate(
				ContractFixtures.Spec("a", 150), new ContractOutcome(0, 10d, false));

			Assert.That(settlement.Lines.Count, Is.EqualTo(1));
			Assert.That(settlement.Lines[0].Kind, Is.EqualTo(SettlementLineKind.BasePay));
			Assert.That(settlement.Total, Is.EqualTo(150));
		}

		[Test]
		public void NoViolationsBonus_RequiresZeroViolations()
		{
			ContractSpec spec = ContractFixtures.Spec("a", 100, objectives: new[] { ContractFixtures.NoViolations(40) });

			Assert.That(SettlementCalculator.Calculate(spec, new ContractOutcome(0, 5d, false)).Total, Is.EqualTo(140));
			Assert.That(SettlementCalculator.Calculate(spec, new ContractOutcome(1, 5d, false)).Total, Is.EqualTo(100));
		}

		[Test]
		public void UnderTimeBonus_RequiresInTimeAndNotOverdue()
		{
			ContractSpec spec = ContractFixtures.Spec("a", 100, objectives: new[] { ContractFixtures.UnderTime(60d, 30) });

			Assert.That(SettlementCalculator.Calculate(spec, new ContractOutcome(0, 59d, false)).Total, Is.EqualTo(130));
			Assert.That(SettlementCalculator.Calculate(spec, new ContractOutcome(0, 61d, false)).Total, Is.EqualTo(100));
			Assert.That(SettlementCalculator.Calculate(spec, new ContractOutcome(0, 30d, true)).Total, Is.EqualTo(100));
		}

		[Test]
		public void Deductions_AreListedOnlyWhenTheyHappened()
		{
			ContractSpec spec = ContractFixtures.Spec("a", 200);

			ContractSettlement clean = SettlementCalculator.Calculate(spec, new ContractOutcome(0, 5d, false));
			ContractSettlement messy = SettlementCalculator.Calculate(
				spec, new ContractOutcome(1, 5d, false, deathCount: 1, shotsFired: 4, propertyDamageCount: 1));

			Assert.That(clean.Lines.Count, Is.EqualTo(1));
			Assert.That(messy.Lines.Count, Is.EqualTo(4));
			Assert.That(messy.Lines[1].Kind, Is.EqualTo(SettlementLineKind.Deduction));
			Assert.That(messy.Lines[1].Amount, Is.EqualTo(-4 * EconomyRules.AmmoCostPerShot));
			Assert.That(messy.Lines[2].Amount, Is.EqualTo(-EconomyRules.PropertyDamageFine));
			Assert.That(messy.Lines[3].Amount, Is.EqualTo(-EconomyRules.DeathPenalty));
			Assert.That(
				messy.Total,
				Is.EqualTo(200 - 4 * EconomyRules.AmmoCostPerShot - EconomyRules.PropertyDamageFine - EconomyRules.DeathPenalty));
		}

		[Test]
		public void Deductions_CannotPushThePayoutBelowZero()
		{
			ContractSettlement settlement = SettlementCalculator.Calculate(
				ContractFixtures.Spec("a", 20), new ContractOutcome(0, 5d, false, deathCount: 3, shotsFired: 50));

			Assert.That(settlement.Total, Is.EqualTo(0));
		}

		[Test]
		public void Run_CountsPropertyDamageOnlyInTransit()
		{
			ContractSpec spec = ContractFixtures.Spec("pd");
			NightCourier.App.CampaignState campaign = NightCourier.App.CampaignState.CreateNew(new ContractBook(new[] { spec }));
			ContractRun run = campaign.Contracts.Accept("pd");

			run.RegisterPropertyDamage();

			Assert.That(run.PropertyDamageCount, Is.EqualTo(0));

			run.CollectPackage();
			run.Depart();
			run.RegisterPropertyDamage();

			Assert.That(run.PropertyDamageCount, Is.EqualTo(1));
			Assert.That(run.CaptureOutcome().PropertyDamageCount, Is.EqualTo(1));
			Assert.That(run.ViolationCount, Is.EqualTo(1));
		}

		[Test]
		public void Total_NeverDropsBelowZero()
		{
			ContractSettlement settlement = new(
				"a",
				new List<SettlementLine>
				{
					new(SettlementLineKind.BasePay, "result.base_pay", 50),
					new(SettlementLineKind.Bonus, "deduction", -200)
				});

			Assert.That(settlement.Total, Is.EqualTo(0));
		}
	}
}
#endif
