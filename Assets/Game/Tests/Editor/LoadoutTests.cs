#if UNITY_INCLUDE_TESTS
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Economy;
using NightCourier.Loadout;
using NightCourier.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class LoadoutTests
	{
		private static CampaignState NewCampaign(long credits)
		{
			CampaignState campaign = CampaignState.CreateNew(new ContractBook(new[] { ContractFixtures.Spec("a") }));

			campaign.Wallet.Add(GameCurrencies.Credits, credits);

			return campaign;
		}

		private static CampaignState RoundTrip(CampaignState original)
		{
			CampaignSave save = JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(original.Capture()));

			return CampaignState.Restore(new ContractBook(new[] { ContractFixtures.Spec("a") }), save);
		}

		[Test]
		public void NewLoadout_CarriesBothWeaponsAndNoMedkits()
		{
			CourierLoadout loadout = NewCampaign(0).Loadout;

			Assert.That(loadout.IsWeaponEquipped(WeaponKind.StunPistol), Is.True);
			Assert.That(loadout.IsWeaponEquipped(WeaponKind.Crowbar), Is.True);
			Assert.That(loadout.MedkitsOwned, Is.EqualTo(0));
		}

		[Test]
		public void Weapons_CanBeStowedAndEquippedAgain()
		{
			CourierLoadout loadout = NewCampaign(0).Loadout;
			int changes = 0;

			loadout.Changed += () => changes++;
			loadout.SetWeaponEquipped(WeaponKind.StunPistol, false);

			Assert.That(loadout.IsWeaponEquipped(WeaponKind.StunPistol), Is.False);
			Assert.That(loadout.IsWeaponEquipped(WeaponKind.Crowbar), Is.True);
			Assert.That(changes, Is.EqualTo(1));

			loadout.SetWeaponEquipped(WeaponKind.StunPistol, true);

			Assert.That(loadout.IsWeaponEquipped(WeaponKind.StunPistol), Is.True);
		}

		[Test]
		public void BuyingAMedkit_SpendsCreditsAndStocksIt()
		{
			CampaignState campaign = NewCampaign(100);

			Assert.That(campaign.Loadout.TryBuyMedkit(), Is.EqualTo(MedkitPurchaseStatus.Purchased));
			Assert.That(campaign.Loadout.MedkitsInStock, Is.EqualTo(1));
			Assert.That(campaign.Credits, Is.EqualTo(100 - LoadoutRules.MedkitPrice));
		}

		[Test]
		public void BuyingAMedkit_FailsWithoutCredits_OrAtCapacity()
		{
			CampaignState poor = NewCampaign(LoadoutRules.MedkitPrice - 1);

			Assert.That(poor.Loadout.TryBuyMedkit(), Is.EqualTo(MedkitPurchaseStatus.InsufficientFunds));
			Assert.That(poor.Loadout.MedkitsOwned, Is.EqualTo(0));
			Assert.That(poor.Credits, Is.EqualTo(LoadoutRules.MedkitPrice - 1));

			CampaignState rich = NewCampaign(1000);

			for (int i = 0; i < LoadoutRules.MedkitCapacity; i++)
			{
				Assert.That(rich.Loadout.TryBuyMedkit(), Is.EqualTo(MedkitPurchaseStatus.Purchased));
			}

			long before = rich.Credits;

			Assert.That(rich.Loadout.TryBuyMedkit(), Is.EqualTo(MedkitPurchaseStatus.AtCapacity));
			Assert.That(rich.Credits, Is.EqualTo(before));
		}

		[Test]
		public void Medkits_MoveBetweenStockAndUtilitySlots()
		{
			CourierLoadout loadout = NewCampaign(1000).Loadout;

			loadout.TryBuyMedkit();
			loadout.TryBuyMedkit();

			Assert.That(loadout.TryEquipMedkit(LoadoutIds.UtilityFirst), Is.True);
			Assert.That(loadout.TryEquipMedkit(LoadoutIds.UtilityFirst), Is.False);
			Assert.That(loadout.TryEquipMedkit(LoadoutIds.UtilitySecond), Is.True);
			Assert.That(loadout.MedkitsEquipped, Is.EqualTo(2));
			Assert.That(loadout.MedkitsInStock, Is.EqualTo(0));
			Assert.That(loadout.TryEquipMedkit(LoadoutIds.Primary), Is.False);

			Assert.That(loadout.TryUnequipMedkit(LoadoutIds.UtilityFirst), Is.True);
			Assert.That(loadout.MedkitsEquipped, Is.EqualTo(1));
			Assert.That(loadout.MedkitsInStock, Is.EqualTo(1));
			Assert.That(loadout.TryUnequipMedkit(LoadoutIds.UtilityFirst), Is.False);
			Assert.That(loadout.MedkitsOwned, Is.EqualTo(2));
		}

		[Test]
		public void UsingAMedkit_ConsumesAnEquippedOneOnly()
		{
			CourierLoadout loadout = NewCampaign(1000).Loadout;

			loadout.TryBuyMedkit();

			Assert.That(loadout.TryConsumeMedkit(), Is.False);
			Assert.That(loadout.MedkitsInStock, Is.EqualTo(1));

			loadout.TryEquipMedkit(LoadoutIds.UtilitySecond);

			Assert.That(loadout.TryConsumeMedkit(), Is.True);
			Assert.That(loadout.MedkitsOwned, Is.EqualTo(0));
			Assert.That(loadout.TryConsumeMedkit(), Is.False);
		}

		[Test]
		public void Save_RestoresWeaponsMedkitsAndStock()
		{
			CampaignState original = NewCampaign(1000);

			original.Loadout.TryBuyMedkit();
			original.Loadout.TryBuyMedkit();
			original.Loadout.TryBuyMedkit();
			original.Loadout.TryEquipMedkit(LoadoutIds.UtilityFirst);
			original.Loadout.SetWeaponEquipped(WeaponKind.Crowbar, false);

			CampaignState restored = RoundTrip(original);

			Assert.That(restored.Loadout.IsWeaponEquipped(WeaponKind.StunPistol), Is.True);
			Assert.That(restored.Loadout.IsWeaponEquipped(WeaponKind.Crowbar), Is.False);
			Assert.That(restored.Loadout.HasMedkit(LoadoutIds.UtilityFirst), Is.True);
			Assert.That(restored.Loadout.MedkitsInStock, Is.EqualTo(2));
			Assert.That(restored.Credits, Is.EqualTo(original.Credits));

			Assert.That(restored.Loadout.TryEquipMedkit(LoadoutIds.UtilitySecond), Is.True);
			Assert.That(restored.Loadout.MedkitsEquipped, Is.EqualTo(2));
		}

		[Test]
		public void Save_FromBeforeTheLoadout_StartsWithTheDefaults()
		{
			CampaignSave old = NewCampaign(10).Capture();

			old.Version = 3;
			old.Loadout = null;

			CampaignState restored = CampaignState.Restore(new ContractBook(new[] { ContractFixtures.Spec("a") }), old);

			Assert.That(restored.Loadout.IsWeaponEquipped(WeaponKind.StunPistol), Is.True);
			Assert.That(restored.Loadout.IsWeaponEquipped(WeaponKind.Crowbar), Is.True);
			Assert.That(restored.Loadout.MedkitsOwned, Is.EqualTo(0));
		}
	}
}
#endif
