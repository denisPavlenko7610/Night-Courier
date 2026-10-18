#if UNITY_INCLUDE_TESTS
using System;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Economy;
using NUnit.Framework;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class ShiftRatingTests
	{
		private static ContractSpec Untimed()
		{
			return ContractFixtures.Spec("untimed", 100);
		}

		private static ContractSpec Timed(double limitSeconds)
		{
			return ContractFixtures.Spec("timed", 100, timeLimit: limitSeconds);
		}

		private static ContractOutcome Outcome(
			int violations = 0,
			double seconds = 10d,
			bool overdue = false,
			int propertyDamage = 0,
			double integrity = 1d)
		{
			return new ContractOutcome(violations, seconds, overdue, 0, 0, propertyDamage, integrity);
		}

		[Test]
		public void CleanDelivery_WithoutADeadline_RatesS_AndSkipsTheTimeAxis()
		{
			ShiftRating rating = ShiftRatingCalculator.Calculate(Untimed(), Outcome());

			Assert.That(rating.DeliveryTime.HasValue, Is.False);
			Assert.That(rating.CargoCondition, Is.EqualTo(ShiftGrade.S));
			Assert.That(rating.ProtocolCompliance, Is.EqualTo(ShiftGrade.S));
			Assert.That(rating.PropertyDamage, Is.EqualTo(ShiftGrade.S));
			Assert.That(rating.Overall, Is.EqualTo(ShiftGrade.S));
		}

		[Test]
		public void DeliveryTime_FollowsTheShareOfTheDeadlineUsed()
		{
			ContractSpec spec = Timed(100d);

			Assert.That(ShiftRatingCalculator.Calculate(spec, Outcome(seconds: 30d)).DeliveryTime, Is.EqualTo(ShiftGrade.S));
			Assert.That(ShiftRatingCalculator.Calculate(spec, Outcome(seconds: 55d)).DeliveryTime, Is.EqualTo(ShiftGrade.A));
			Assert.That(ShiftRatingCalculator.Calculate(spec, Outcome(seconds: 75d)).DeliveryTime, Is.EqualTo(ShiftGrade.B));
			Assert.That(ShiftRatingCalculator.Calculate(spec, Outcome(seconds: 95d)).DeliveryTime, Is.EqualTo(ShiftGrade.C));
			Assert.That(ShiftRatingCalculator.Calculate(spec, Outcome(seconds: 95d, overdue: true)).DeliveryTime, Is.EqualTo(ShiftGrade.F));
		}

		[Test]
		public void CargoCondition_FollowsTheIntegrity()
		{
			Assert.That(ShiftRatingCalculator.Calculate(Untimed(), Outcome(integrity: 1d)).CargoCondition, Is.EqualTo(ShiftGrade.S));
			Assert.That(ShiftRatingCalculator.Calculate(Untimed(), Outcome(integrity: 0.9d)).CargoCondition, Is.EqualTo(ShiftGrade.A));
			Assert.That(ShiftRatingCalculator.Calculate(Untimed(), Outcome(integrity: 0.7d)).CargoCondition, Is.EqualTo(ShiftGrade.B));
			Assert.That(ShiftRatingCalculator.Calculate(Untimed(), Outcome(integrity: 0.5d)).CargoCondition, Is.EqualTo(ShiftGrade.C));
			Assert.That(ShiftRatingCalculator.Calculate(Untimed(), Outcome(integrity: 0.2d)).CargoCondition, Is.EqualTo(ShiftGrade.D));
		}

		[Test]
		public void PropertyDamage_IsNotCountedAsAProtocolViolation()
		{
			ShiftRating rating = ShiftRatingCalculator.Calculate(Untimed(), Outcome(violations: 2, propertyDamage: 2));

			Assert.That(rating.ProtocolCompliance, Is.EqualTo(ShiftGrade.S));
			Assert.That(rating.PropertyDamage, Is.EqualTo(ShiftGrade.C));
		}

		[Test]
		public void ProtocolViolations_LowerTheGrade()
		{
			Assert.That(ShiftRatingCalculator.Calculate(Untimed(), Outcome(violations: 1)).ProtocolCompliance, Is.EqualTo(ShiftGrade.B));
			Assert.That(ShiftRatingCalculator.Calculate(Untimed(), Outcome(violations: 2)).ProtocolCompliance, Is.EqualTo(ShiftGrade.C));
			Assert.That(ShiftRatingCalculator.Calculate(Untimed(), Outcome(violations: 9)).ProtocolCompliance, Is.EqualTo(ShiftGrade.F));
		}

		[Test]
		public void Overall_IsTheRoundedAverageOfTheAxes()
		{
			ShiftRating rating = new(ShiftGrade.A, ShiftGrade.S, ShiftGrade.C, ShiftGrade.F);

			Assert.That(rating.Overall, Is.EqualTo(ShiftGrade.B));
			Assert.That(new ShiftRating(null, ShiftGrade.B, ShiftGrade.B, ShiftGrade.B).Overall, Is.EqualTo(ShiftGrade.B));
			Assert.That(new ShiftRating(null, ShiftGrade.S, ShiftGrade.S, ShiftGrade.A).Overall, Is.EqualTo(ShiftGrade.S));
		}

		[Test]
		public void Settlement_CarriesTheRating()
		{
			ContractSettlement settlement = SettlementCalculator.Calculate(Untimed(), Outcome(violations: 1, propertyDamage: 1));

			Assert.That(settlement.Rating.PropertyDamage, Is.EqualTo(ShiftGrade.B));
			Assert.That(settlement.Rating.ProtocolCompliance, Is.EqualTo(ShiftGrade.S));
		}

		[Test]
		public void Run_RecordsCargoIntegrityOnlyInTransit()
		{
			ContractSpec spec = ContractFixtures.Spec("cargo");
			CampaignState campaign = CampaignState.CreateNew(new ContractBook(new[] { spec }));
			ContractRun run = campaign.Contracts.Accept("cargo");

			run.RecordCargoIntegrity(0.3d);

			Assert.That(run.CargoIntegrity, Is.EqualTo(1d));

			run.CollectPackage();
			run.Depart();
			run.RecordCargoIntegrity(0.6d);
			run.RecordCargoIntegrity(7d);
			run.RecordCargoIntegrity(0.6d);

			Assert.That(run.CaptureOutcome().CargoIntegrity, Is.EqualTo(0.6d));
		}

		[Test]
		public void Rank_StartsAsTrainee_AndPromotesWithShiftRatings()
		{
			CourierRank rank = new();

			Assert.That(rank.Level, Is.EqualTo(1));
			Assert.That(rank.TitleKey, Is.EqualTo("rank.trainee"));

			RankChange first = rank.Apply(new ShiftRating(null, ShiftGrade.S, ShiftGrade.S, ShiftGrade.S));

			Assert.That(first.ExperienceGained, Is.EqualTo(CourierRank.ExperienceFor(ShiftGrade.S)));
			Assert.That(first.Promoted, Is.True);
			Assert.That(rank.Level, Is.EqualTo(2));
			Assert.That(rank.TitleKey, Is.EqualTo("rank.courier"));
			Assert.That(rank.ExperienceIntoLevel, Is.EqualTo(20));

			RankChange failed = rank.Apply(new ShiftRating(null, ShiftGrade.F, ShiftGrade.F, ShiftGrade.F));

			Assert.That(failed.ExperienceGained, Is.EqualTo(0));
			Assert.That(failed.Promoted, Is.False);
			Assert.That(rank.TotalExperience, Is.EqualTo(120));
		}

		[Test]
		public void Rank_StopsAtTheTopLevel()
		{
			CourierRank rank = new();

			for (int i = 0; i < 10; i++)
			{
				rank.Apply(new ShiftRating(null, ShiftGrade.S, ShiftGrade.S, ShiftGrade.S));
			}

			Assert.That(rank.IsMaxLevel, Is.True);
			Assert.That(rank.TitleKey, Is.EqualTo("rank.hazard_courier"));
		}

		[Test]
		public void Save_RestoresTheRank_AndOldSavesStartAtTrainee()
		{
			ContractBook book = new(new[] { ContractFixtures.Spec("a") });
			CampaignState original = CampaignState.CreateNew(book);

			original.Rank.Apply(new ShiftRating(null, ShiftGrade.A, ShiftGrade.A, ShiftGrade.A));
			original.Rank.Apply(new ShiftRating(null, ShiftGrade.A, ShiftGrade.A, ShiftGrade.A));

			CampaignSave save = JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(original.Capture()));
			CampaignState restored = CampaignState.Restore(book, save);

			Assert.That(restored.Rank.TotalExperience, Is.EqualTo(200));
			Assert.That(restored.Rank.Level, Is.EqualTo(original.Rank.Level));

			CampaignSave old = CampaignState.CreateNew(book).Capture();

			old.Version = 4;
			old.Rank = null;

			Assert.That(CampaignState.Restore(book, old).Rank.Level, Is.EqualTo(1));
		}
	}
}
#endif
