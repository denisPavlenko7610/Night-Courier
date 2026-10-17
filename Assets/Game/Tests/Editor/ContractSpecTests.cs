#if UNITY_INCLUDE_TESTS
using System;
using NightCourier.Contracts;
using NUnit.Framework;

namespace NightCourier.Tests
{
	public sealed class ContractSpecTests
	{
		[Test]
		public void Constructor_InvalidValues_Throw()
		{
			Assert.Throws<ArgumentException>(() => ContractFixtures.Spec(""));
			Assert.Throws<ArgumentOutOfRangeException>(() => ContractFixtures.Spec("a", reward: 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => ContractFixtures.Spec("a", timeLimit: -1d));
			Assert.Throws<ArgumentOutOfRangeException>(() => ContractFixtures.Spec("a", timeLimit: double.NaN));
			Assert.Throws<ArgumentException>(() => ContractFixtures.Spec("a", prerequisites: new[] { "a" }));
			Assert.Throws<ArgumentException>(() => ContractFixtures.Spec("a", prerequisites: new[] { "b", "b" }));
			Assert.Throws<ArgumentException>(() => ContractFixtures.Spec(
				"a", objectives: new[] { ContractFixtures.NoViolations(), ContractFixtures.NoViolations() }));
		}

		[Test]
		public void ObjectiveSpec_TimeObjectiveNeedsThreshold()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => ContractFixtures.UnderTime(0d));
			Assert.Throws<ArgumentOutOfRangeException>(() => ContractFixtures.NoViolations(0));
		}

		[Test]
		public void Book_RejectsDuplicateAndUnknownPrerequisites()
		{
			Assert.Throws<ArgumentException>(() => new ContractBook(new[] { ContractFixtures.Spec("a"), ContractFixtures.Spec("a") }));
			Assert.Throws<ArgumentException>(() => new ContractBook(new[]
			{
				ContractFixtures.Spec("a", prerequisites: new[] { "missing" })
			}));
		}

		[Test]
		public void Book_LooksUpById()
		{
			ContractBook book = ContractFixtures.Chain();

			Assert.That(book.All.Count, Is.EqualTo(2));
			Assert.That(book.Get("second").BaseReward, Is.EqualTo(200));
			Assert.That(book.TryGet("nope", out _), Is.False);
			Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => book.Get("nope"));
		}
	}
}
#endif
