#if UNITY_INCLUDE_TESTS
using System;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Economy;
using NUnit.Framework;
using RD.Stats;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class UpgradeTests
	{
		private static UpgradeSpec Vest()
		{
			return new UpgradeSpec(
				"vest", "t", "d", CourierStats.MaxHealthId, StatModifierKind.Flat, 25d, new long[] { 100, 200, 350 });
		}

		private static UpgradeSpec Boots()
		{
			return new UpgradeSpec(
				"boots", "t", "d", CourierStats.MoveSpeedId, StatModifierKind.AdditivePercent, 0.08d, new long[] { 120, 240 });
		}

		private static UpgradeSpec Pouch()
		{
			return new UpgradeSpec(
				"pouch", "t", "d", CourierStats.ReserveAmmoId, StatModifierKind.Flat, 8d, new long[] { 80 });
		}

		private static UpgradeBook Book()
		{
			return new UpgradeBook(new[] { Vest(), Boots(), Pouch() });
		}

		private static CampaignState NewCampaign(long credits)
		{
			CampaignState campaign = CampaignState.CreateNew(new ContractBook(new[] { ContractFixtures.Spec("a") }), Book());

			campaign.Wallet.Add(GameCurrencies.Credits, credits);

			return campaign;
		}

		[Test]
		public void Spec_RejectsMissingLevels_BadAmounts_AndMultiplicativeKind()
		{
			Assert.Throws<ArgumentException>(() => new UpgradeSpec(
				"a", "t", "d", CourierStats.MaxHealthId, StatModifierKind.Flat, 5d, Array.Empty<long>()));
			Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeSpec(
				"a", "t", "d", CourierStats.MaxHealthId, StatModifierKind.Flat, 0d, new long[] { 10 }));
			Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeSpec(
				"a", "t", "d", CourierStats.MaxHealthId, StatModifierKind.Flat, 5d, new long[] { 0 }));
			Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeSpec(
				"a", "t", "d", CourierStats.MaxHealthId, StatModifierKind.MultiplicativePercent, 0.1d, new long[] { 10 }));
		}

		[Test]
		public void Spec_PriceOfNextLevel_FollowsTheLevels()
		{
			UpgradeSpec vest = Vest();

			Assert.That(vest.MaxLevel, Is.EqualTo(3));
			Assert.That(vest.PriceOfNextLevel(0), Is.EqualTo(100));
			Assert.That(vest.PriceOfNextLevel(2), Is.EqualTo(350));
			Assert.Throws<ArgumentOutOfRangeException>(() => vest.PriceOfNextLevel(3));
		}

		[Test]
		public void Book_RejectsDuplicateIds()
		{
			Assert.Throws<ArgumentException>(() => new UpgradeBook(new[] { Vest(), Vest() }));
		}

		[Test]
		public void Stats_StartAtTheirBaseValues()
		{
			CourierStats stats = new();

			Assert.That(stats.MaxHealth, Is.EqualTo(CourierStats.BaseMaxHealth));
			Assert.That(stats.MoveSpeedMultiplier, Is.EqualTo(1f));
			Assert.That(stats.ReserveAmmoBonus, Is.EqualTo(0));
		}

		[Test]
		public void Purchase_AppliesTheStatModifier()
		{
			CampaignState campaign = NewCampaign(1000);

			Assert.That(campaign.Upgrades.TryPurchase("vest"), Is.EqualTo(UpgradePurchaseStatus.Purchased));
			Assert.That(campaign.Upgrades.TryPurchase("vest"), Is.EqualTo(UpgradePurchaseStatus.Purchased));
			Assert.That(campaign.Upgrades.TryPurchase("boots"), Is.EqualTo(UpgradePurchaseStatus.Purchased));
			Assert.That(campaign.Upgrades.TryPurchase("pouch"), Is.EqualTo(UpgradePurchaseStatus.Purchased));

			Assert.That(campaign.Stats.MaxHealth, Is.EqualTo(150d));
			Assert.That(campaign.Stats.MoveSpeedMultiplier, Is.EqualTo(1.08f).Within(0.0001f));
			Assert.That(campaign.Stats.ReserveAmmoBonus, Is.EqualTo(8));
			Assert.That(campaign.Credits, Is.EqualTo(1000 - 100 - 200 - 120 - 80));
		}

		[Test]
		public void Purchase_WithoutEnoughCredits_ChangesNothing()
		{
			CampaignState campaign = NewCampaign(99);

			Assert.That(campaign.Upgrades.TryPurchase("vest"), Is.EqualTo(UpgradePurchaseStatus.InsufficientFunds));
			Assert.That(campaign.Upgrades.GetLevel("vest"), Is.EqualTo(0));
			Assert.That(campaign.Credits, Is.EqualTo(99));
			Assert.That(campaign.Stats.MaxHealth, Is.EqualTo(CourierStats.BaseMaxHealth));
		}

		[Test]
		public void Purchase_StopsAtTheMaximumLevel()
		{
			CampaignState campaign = NewCampaign(1000);

			Assert.That(campaign.Upgrades.TryPurchase("pouch"), Is.EqualTo(UpgradePurchaseStatus.Purchased));
			Assert.That(campaign.Upgrades.TryPurchase("pouch"), Is.EqualTo(UpgradePurchaseStatus.MaxLevel));
			Assert.That(campaign.Credits, Is.EqualTo(920));
			Assert.That(campaign.Upgrades.IsMaxed(Pouch()), Is.True);
		}

		[Test]
		public void Upgrades_TargetingAnUnknownStat_AreRejected()
		{
			UpgradeSpec bogus = new("x", "t", "d", "courier.unknown", StatModifierKind.Flat, 1d, new long[] { 10 });

			Assert.Throws<ArgumentException>(() => CampaignState.CreateNew(
				new ContractBook(new[] { ContractFixtures.Spec("a") }), new UpgradeBook(new[] { bogus })));
		}

		[Test]
		public void Save_RestoresLevelsStatsAndCredits()
		{
			CampaignState original = NewCampaign(1000);

			original.Upgrades.TryPurchase("vest");
			original.Upgrades.TryPurchase("vest");
			original.Upgrades.TryPurchase("pouch");

			CampaignSave save = JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(original.Capture()));
			CampaignState restored = CampaignState.Restore(
				new ContractBook(new[] { ContractFixtures.Spec("a") }), save, Book());

			Assert.That(restored.Upgrades.GetLevel("vest"), Is.EqualTo(2));
			Assert.That(restored.Upgrades.GetLevel("pouch"), Is.EqualTo(1));
			Assert.That(restored.Upgrades.GetLevel("boots"), Is.EqualTo(0));
			Assert.That(restored.Stats.MaxHealth, Is.EqualTo(150d));
			Assert.That(restored.Stats.ReserveAmmoBonus, Is.EqualTo(8));
			Assert.That(restored.Credits, Is.EqualTo(original.Credits));
		}

		[Test]
		public void Save_IgnoresUnknownUpgrades_AndClampsLevels()
		{
			CampaignState original = NewCampaign(10);
			CampaignSave save = original.Capture();

			save.Upgrades.Add(new UpgradeLevelSave { Id = "removed", Level = 3 });
			save.Upgrades.Add(new UpgradeLevelSave { Id = "pouch", Level = 9 });

			CampaignState restored = CampaignState.Restore(
				new ContractBook(new[] { ContractFixtures.Spec("a") }), save, Book());

			Assert.That(restored.Upgrades.GetLevel("pouch"), Is.EqualTo(1));
			Assert.That(restored.Upgrades.GetLevel("removed"), Is.EqualTo(0));
		}

		[Test]
		public void Save_FromTheVersionBeforeUpgrades_StillLoads()
		{
			CampaignSave old = NewCampaign(10).Capture();

			old.Version = 2;
			old.Upgrades = null;

			CampaignState restored = CampaignState.Restore(
				new ContractBook(new[] { ContractFixtures.Spec("a") }), old, Book());

			Assert.That(restored.Upgrades.GetLevel("vest"), Is.EqualTo(0));
			Assert.That(restored.Credits, Is.EqualTo(10));
		}
	}
}
#endif
