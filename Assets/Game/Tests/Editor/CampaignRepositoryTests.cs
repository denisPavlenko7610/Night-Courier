#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Threading.Tasks;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Economy;
using NUnit.Framework;
using RD.Save;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class CampaignRepositoryTests
	{
		private string _root;

		[SetUp]
		public void SetUp()
		{
			_root = Path.Combine(Application.temporaryCachePath, "nc-campaign-tests-" + Guid.NewGuid().ToString("N"));
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(_root))
			{
				Directory.Delete(_root, true);
			}
			else if (File.Exists(_root))
			{
				File.Delete(_root);
			}
		}

		private string SlotPath => Path.Combine(_root, CampaignRepository.SlotId + ".json");

		private CampaignRepository NewRepository()
		{
			return new CampaignRepository(new SaveStore(_root), ContractFixtures.Chain(), UpgradeBook.Empty);
		}

		private static async Task<Exception> Capture(Func<Awaitable> action)
		{
			try
			{
				await action();

				return null;
			}
			catch (Exception exception)
			{
				return exception;
			}
		}

		private static async Task Finish(CampaignRepository repository, string contractId)
		{
			ContractRun run = repository.Current.Contracts.Accept(contractId);
			run.CollectPackage();
			run.Depart();
			run.Deliver();
			await repository.Current.Contracts.SettleAsync();
		}

		[Test]
		public async Task WithoutAFile_ThereIsNoSave_AndContinueFailsWithAKnownException()
		{
			CampaignRepository repository = NewRepository();

			await repository.RefreshAsync();

			Assert.That(repository.HasSave, Is.False);
			Assert.That(await Capture(() => repository.ContinueAsync()), Is.InstanceOf<FileNotFoundException>());
			Assert.That(repository.Current, Is.Null);
		}

		[Test]
		public async Task SaveThenContinue_RestoresProgressInAFreshRepository()
		{
			CampaignRepository first = NewRepository();
			first.StartNew();
			await Finish(first, "first");
			await first.SaveAsync();

			CampaignRepository second = NewRepository();
			await second.RefreshAsync();
			await second.ContinueAsync();

			Assert.That(first.HasSave, Is.True);
			Assert.That(second.HasSave, Is.True);
			Assert.That(second.Current.Credits, Is.EqualTo(100));
			Assert.That(second.Current.Contracts.GetAvailable()[0].Id, Is.EqualTo("second"));
		}

		[Test]
		public async Task SavingWithAContractInProgress_RestoresItAsAvailableAgain()
		{
			CampaignRepository first = NewRepository();
			first.StartNew();
			first.Current.Contracts.Accept("first").CollectPackage();
			await first.SaveAsync();

			CampaignRepository second = NewRepository();
			await second.ContinueAsync();

			Assert.That(second.Current.Contracts.ActiveRun, Is.Null);
			Assert.That(second.Current.Contracts.GetAvailable()[0].Id, Is.EqualTo("first"));
			Assert.That(second.Current.Credits, Is.EqualTo(0));
		}

		[Test]
		public async Task TwoSavesStartedTogether_LeaveALoadableFile()
		{
			CampaignRepository first = NewRepository();
			first.StartNew();
			await Finish(first, "first");

			Awaitable one = first.SaveAsync();
			Awaitable two = first.SaveAsync();

			await one;
			await two;

			CampaignRepository second = NewRepository();
			await second.ContinueAsync();

			Assert.That(second.Current.Credits, Is.EqualTo(100));
		}

		[TestCase("")]
		[TestCase("{ this is not json")]
		[TestCase("[]")]
		public async Task CorruptFile_FailsWithInvalidSaveData_NotWithAnUnknownException(string content)
		{
			CampaignRepository repository = NewRepository();
			Directory.CreateDirectory(_root);
			File.WriteAllText(SlotPath, content);

			await repository.RefreshAsync();

			Assert.That(repository.HasSave, Is.True);
			Assert.That(await Capture(() => repository.ContinueAsync()), Is.InstanceOf<InvalidSaveDataException>());
			Assert.That(repository.Current, Is.Null);
		}

		[Test]
		public async Task TruncatedFile_FailsWithInvalidSaveData()
		{
			CampaignRepository first = NewRepository();
			first.StartNew();
			await first.SaveAsync();

			string json = File.ReadAllText(SlotPath);
			File.WriteAllText(SlotPath, json.Substring(0, json.Length / 2));

			CampaignRepository second = NewRepository();

			Assert.That(await Capture(() => second.ContinueAsync()), Is.InstanceOf<InvalidSaveDataException>());
		}

		[Test]
		public async Task SaveFromANewerVersion_IsRejected()
		{
			CampaignRepository first = NewRepository();
			first.StartNew();
			await first.SaveAsync();

			string newer = "\"Version\":" + (CampaignSave.CurrentVersion + 1);
			string json = File.ReadAllText(SlotPath).Replace("\"Version\":" + CampaignSave.CurrentVersion, newer);

			Assert.That(json, Does.Contain(newer));

			File.WriteAllText(SlotPath, json);

			CampaignRepository second = NewRepository();

			Assert.That(await Capture(() => second.ContinueAsync()), Is.InstanceOf<InvalidSaveDataException>());
		}

		[Test]
		public async Task SavingWithoutACampaign_Throws()
		{
			CampaignRepository repository = NewRepository();

			Assert.That(await Capture(() => repository.SaveAsync()), Is.InstanceOf<InvalidOperationException>());
		}

		[Test]
		public async Task WriteFailure_KeepsHasSaveFalse_AndTheCampaignUsable()
		{
			File.WriteAllText(_root, "this file blocks the save directory");

			CampaignRepository repository = NewRepository();
			repository.StartNew();
			await Finish(repository, "first");

			Assert.That(await Capture(() => repository.SaveAsync()), Is.Not.Null);
			Assert.That(repository.HasSave, Is.False);
			Assert.That(repository.Current.Credits, Is.EqualTo(100));
		}
	}
}
#endif
