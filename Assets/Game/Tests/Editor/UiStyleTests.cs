#if UNITY_INCLUDE_TESTS
using NightCourier.Contracts;
using NightCourier.UI;
using NUnit.Framework;

namespace NightCourier.Tests
{
	public sealed class UiStyleTests
	{
		[TestCase(260L, "₡260")]
		[TestCase(0L, "₡0")]
		[TestCase(-20L, "-₡20")]
		public void Money_UsesTheCreditSymbolAndKeepsTheSign(long amount, string expected)
		{
			Assert.That(UiStyle.Money(amount), Is.EqualTo(expected));
		}

		[Test]
		public void SignedMoney_PrefixesGainsWithAPlus()
		{
			Assert.That(UiStyle.SignedMoney(75), Is.EqualTo("+₡75"));
			Assert.That(UiStyle.SignedMoney(-20), Is.EqualTo("-₡20"));
		}

		[TestCase(0, 3, "□□□")]
		[TestCase(2, 3, "■■□")]
		[TestCase(3, 3, "■■■")]
		[TestCase(9, 3, "■■■")]
		[TestCase(-1, 2, "□□")]
		public void Pips_ShowTheFilledShareAndNeverOverflow(int filled, int total, string expected)
		{
			Assert.That(UiStyle.Pips(filled, total), Is.EqualTo(expected));
		}

		[Test]
		public void IntegrityColor_FollowsTheThresholds()
		{
			Assert.That(UiStyle.IntegrityColor(1f), Is.EqualTo(UiStyle.GoodColor));
			Assert.That(UiStyle.IntegrityColor(UiStyle.GoodIntegrity), Is.EqualTo(UiStyle.GoodColor));
			Assert.That(UiStyle.IntegrityColor(0.5f), Is.EqualTo(UiStyle.WarningColor));
			Assert.That(UiStyle.IntegrityColor(0.1f), Is.EqualTo(UiStyle.DangerColor));
		}

		[Test]
		public void GradeAndRiskColors_SeparateGoodFromBad()
		{
			Assert.That(UiStyle.GradeColor(ShiftGrade.S), Is.EqualTo(UiStyle.GoodColor));
			Assert.That(UiStyle.GradeColor(ShiftGrade.C), Is.EqualTo(UiStyle.WarningColor));
			Assert.That(UiStyle.GradeColor(ShiftGrade.F), Is.EqualTo(UiStyle.DangerColor));
			Assert.That(UiStyle.RiskColor(ContractRisk.Low), Is.EqualTo(UiStyle.GoodColor));
			Assert.That(UiStyle.RiskColor(ContractRisk.High), Is.EqualTo(UiStyle.DangerColor));
		}

		[Test]
		public void CaptionAndHeading_UppercaseAndTint()
		{
			Assert.That(UiStyle.Caption("Base pay"), Is.EqualTo("<color=" + UiStyle.MutedColor + ">BASE PAY</color>"));
			Assert.That(UiStyle.Heading("Debrief"), Is.EqualTo("<color=" + UiStyle.AccentColor + ">DEBRIEF</color>"));
		}

		[TestCase("contract.fragile.title", "contract.fragile.notes")]
		[TestCase("contract.restricted_cargo.title", "contract.restricted_cargo.notes")]
		public void ContractCompanionKeys_ReplaceTheTitleSuffix(string titleKey, string notesKey)
		{
			Assert.That(ContractKeys.NotesKey(titleKey), Is.EqualTo(notesKey));
			Assert.That(ContractKeys.OrderKey(titleKey), Does.EndWith(".order"));
			Assert.That(ContractKeys.MemoKey(titleKey), Does.EndWith(".memo"));
		}

		[Test]
		public void ContractCompanionKeys_RejectKeysWithoutTheTitleSuffix()
		{
			Assert.Throws<System.ArgumentException>(() => ContractKeys.NotesKey("contract.fragile.description"));
			Assert.Throws<System.ArgumentException>(() => ContractKeys.LocationKey(string.Empty));
		}
	}
}
#endif
