#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
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
