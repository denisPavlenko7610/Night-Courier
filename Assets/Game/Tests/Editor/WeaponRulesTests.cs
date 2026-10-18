#if UNITY_INCLUDE_TESTS
using System;
using NightCourier.Contracts;
using NightCourier.Weapons;
using NUnit.Framework;

namespace NightCourier.Tests
{
	public sealed class WeaponRulesTests
	{
		[Test]
		public void FreeHands_AllowEveryWeapon()
		{
			Assert.That(WeaponRules.CanUse(WeaponKind.StunPistol, false, false), Is.True);
			Assert.That(WeaponRules.CanUse(WeaponKind.Crowbar, false, false), Is.True);
		}

		[Test]
		public void HeldPackage_AllowsOnlyTheOneHandedPistol()
		{
			Assert.That(WeaponRules.CanUse(WeaponKind.StunPistol, true, false), Is.True);
			Assert.That(WeaponRules.CanUse(WeaponKind.Crowbar, true, false), Is.False);
		}

		[Test]
		public void HeavyCarry_BlocksEveryWeapon()
		{
			Assert.That(WeaponRules.CanUse(WeaponKind.StunPistol, false, true), Is.False);
			Assert.That(WeaponRules.CanUse(WeaponKind.Crowbar, false, true), Is.False);
			Assert.That(WeaponRules.CanUse(WeaponKind.StunPistol, true, true), Is.False);
		}

		[Test]
		public void NameKeys_CoverEveryWeapon()
		{
			foreach (WeaponKind kind in Enum.GetValues(typeof(WeaponKind)))
			{
				Assert.That(WeaponRules.NameKey(kind), Does.StartWith("weapon."));
			}
		}

		[Test]
		public void Run_CountsShotsOnlyInTransit()
		{
			ContractSpec spec = ContractFixtures.Spec("shots");
			NightCourier.App.CampaignState campaign = NightCourier.App.CampaignState.CreateNew(new ContractBook(new[] { spec }));
			ContractRun run = campaign.Contracts.Accept("shots");

			run.RegisterShot();

			Assert.That(run.ShotsFired, Is.EqualTo(0));

			run.CollectPackage();
			run.Depart();
			run.RegisterShot();
			run.RegisterShot();

			Assert.That(run.ShotsFired, Is.EqualTo(2));
			Assert.That(run.CaptureOutcome().ShotsFired, Is.EqualTo(2));
		}
	}
}
#endif
