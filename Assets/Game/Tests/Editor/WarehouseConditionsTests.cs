#if UNITY_INCLUDE_TESTS
using System.Linq;
using NightCourier.World;
using NUnit.Framework;
using RD.Conditions;

namespace NightCourier.Tests
{
	public sealed class WarehouseConditionsTests
	{
		private ConditionEngine _engine;
		private WarehouseFacts _facts;

		[SetUp]
		public void SetUp()
		{
			_engine = WarehouseConditions.CreateEngine();
			_facts = new WarehouseFacts();
		}

		private ConditionEvaluation Evaluate(ConditionRule rule)
		{
			return _engine.Evaluate(rule, WarehouseConditions.CreateContext(_facts));
		}

		[Test]
		public void MaintenanceDoor_RequiresPower()
		{
			ConditionRule rule = WarehouseConditions.MaintenanceDoor();

			ConditionEvaluation locked = Evaluate(rule);

			Assert.That(locked.IsSatisfied, Is.False);
			Assert.That(locked.Reasons.Single().Code, Is.EqualTo(WarehouseFactEvaluator.PowerOffReason));

			_facts.SetPower(true);

			Assert.That(Evaluate(rule).IsSatisfied, Is.True);
		}

		[Test]
		public void ScannerHatch_RequiresPowerAndScan()
		{
			ConditionRule rule = WarehouseConditions.ScannerHatch();

			Assert.That(Evaluate(rule).IsSatisfied, Is.False);

			_facts.SetCargoScanned(true);
			ConditionEvaluation withoutPower = Evaluate(rule);

			Assert.That(withoutPower.IsSatisfied, Is.False);
			Assert.That(
				withoutPower.Reasons.Select(reason => reason.Code),
				Is.EquivalentTo(new[] { WarehouseFactEvaluator.PowerOffReason })
			);

			_facts.SetPower(true);

			Assert.That(Evaluate(rule).IsSatisfied, Is.True);
		}

		[Test]
		public void ScannerHatch_ReportsEveryMissingFact()
		{
			ConditionEvaluation evaluation = Evaluate(WarehouseConditions.ScannerHatch());

			Assert.That(
				evaluation.Reasons.Select(reason => reason.Code),
				Is.EquivalentTo(new[]
				{
					WarehouseFactEvaluator.PowerOffReason,
					WarehouseFactEvaluator.CargoNotScannedReason
				})
			);
		}

		[Test]
		public void Facts_RaiseChangedOnlyWhenValueChanges()
		{
			int changes = 0;
			_facts.Changed += () => changes++;

			_facts.SetPower(true);
			_facts.SetPower(true);
			_facts.SetCargoScanned(true);
			_facts.SetCargoScanned(true);
			_facts.SetPower(false);

			Assert.That(changes, Is.EqualTo(3));
		}

		[Test]
		public void LeverReady_RequiresBatteryOnlyWhenBatteryIsRequired()
		{
			Assert.That(_facts.LeverReady, Is.True);

			_facts.SetBatteryRequired(true);

			Assert.That(_facts.LeverReady, Is.False);

			_facts.SetBatteryInserted(true);

			Assert.That(_facts.LeverReady, Is.True);

			_facts.SetPower(true);

			Assert.That(_facts.LeverReady, Is.False);
		}

		[Test]
		public void CreateContext_WithoutFacts_Throws()
		{
			Assert.Throws<System.ArgumentNullException>(() => WarehouseConditions.CreateContext(null));
		}
	}
}
#endif
