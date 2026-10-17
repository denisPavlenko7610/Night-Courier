#if UNITY_INCLUDE_TESTS
using System.Threading.Tasks;
using NightCourier.App;
using NightCourier.Contracts;
using NUnit.Framework;
using RD.Save;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class CampaignStateTests
	{
		private static CampaignSave RoundTrip(CampaignSave save)
		{
			return JsonUtility.FromJson<CampaignSave>(JsonUtility.ToJson(save));
		}

		[Test]
		public async Task CaptureRestore_PreservesCreditsAndProgress()
		{
			ContractBook book = ContractFixtures.Chain();
			CampaignState original = CampaignState.CreateNew(book);

			ContractRun run = original.Contracts.Accept("first");
			run.CollectPackage();
			run.Depart();
			run.Deliver();
			await original.Contracts.SettleAsync();

			CampaignState restored = CampaignState.Restore(book, RoundTrip(original.Capture()));

			Assert.That(restored.Credits, Is.EqualTo(100));
			Assert.That(restored.Contracts.CompletedCount, Is.EqualTo(1));
			Assert.That(restored.Contracts.GetAvailable()[0].Id, Is.EqualTo("second"));
		}

		[Test]
		public async Task Restore_KeepsRewardLedger_SoACompletedGrantIsNotPaidTwice()
		{
			ContractBook book = new(new[] { ContractFixtures.Spec("loop", 80, repeatable: true) });
			CampaignState original = CampaignState.CreateNew(book);
			ContractRun run = original.Contracts.Accept("loop");
			run.CollectPackage();
			run.Depart();
			run.Deliver();
			await original.Contracts.SettleAsync();

			CampaignState restored = CampaignState.Restore(book, RoundTrip(original.Capture()));
			ContractRun again = restored.Contracts.Accept("loop");
			again.CollectPackage();
			again.Depart();
			again.Deliver();
			await restored.Contracts.SettleAsync();

			Assert.That(restored.Credits, Is.EqualTo(160));
		}

		[Test]
		public void Restore_DropsUnfinishedContracts()
		{
			ContractBook book = ContractFixtures.Chain();
			CampaignState original = CampaignState.CreateNew(book);
			original.Contracts.Accept("first").CollectPackage();

			CampaignState restored = CampaignState.Restore(book, RoundTrip(original.Capture()));

			Assert.That(restored.Contracts.ActiveRun, Is.Null);
			Assert.That(restored.Contracts.GetAvailable()[0].Id, Is.EqualTo("first"));
		}

		[Test]
		public void Restore_RejectsIncompleteOrOldSaves()
		{
			ContractBook book = ContractFixtures.Chain();
			CampaignSave valid = CampaignState.CreateNew(book).Capture();

			Assert.Throws<InvalidSaveDataException>(() => CampaignState.Restore(book, null));
			Assert.Throws<InvalidSaveDataException>(() => CampaignState.Restore(book, new CampaignSave { Version = 1 }));

			CampaignSave missingWallet = RoundTrip(valid);
			missingWallet.Wallet = null;

			Assert.Throws<InvalidSaveDataException>(() => CampaignState.Restore(book, missingWallet));
		}

		[Test]
		public async Task Restore_IgnoresContractsRemovedFromTheCatalog()
		{
			ContractBook full = ContractFixtures.Chain();
			CampaignState original = CampaignState.CreateNew(full);
			ContractRun run = original.Contracts.Accept("first");
			run.CollectPackage();
			run.Depart();
			run.Deliver();
			await original.Contracts.SettleAsync();

			ContractBook reduced = new(new[] { ContractFixtures.Spec("first", 100) });
			CampaignState restored = CampaignState.Restore(reduced, RoundTrip(original.Capture()));

			Assert.That(restored.Credits, Is.EqualTo(100));
		}
	}
}
#endif
