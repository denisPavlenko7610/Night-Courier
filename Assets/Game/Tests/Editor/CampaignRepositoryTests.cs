#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
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
		public async Task FailedFinalSettlementSave_RetainsRecovery_AndRetryDoesNotDuplicateMoneyOrRank()
		{
			var storage = new FailingFinalSaveStorage(_root);
			CampaignRepository repository = new(new SaveStore(storage, new UnityJsonSaveSerializer()), ContractFixtures.Chain(), UpgradeBook.Empty);
			repository.StartNew();
			ContractRun run = repository.Current.Contracts.Accept("first");
			run.CollectPackage();
			run.Depart();
			run.Deliver();
			repository.Current.PrepareSettlement();
			Assert.That(await Capture(() => repository.RecoverSettlementAsync()), Is.InstanceOf<IOException>());
			Assert.That(repository.Current.Credits, Is.EqualTo(100));
			Assert.That(repository.Current.Rank.TotalExperience, Is.EqualTo(120));
			Assert.That(repository.Current.Contracts.PendingSettlement, Is.Not.Null);
			CampaignRepository restarted = NewRepository();
			await restarted.ContinueAsync();
			Assert.That(restarted.Current.Credits, Is.EqualTo(100));
			Assert.That(restarted.Current.Rank.TotalExperience, Is.EqualTo(120));
			await repository.RecoverSettlementAsync();
			Assert.That(repository.Current.Credits, Is.EqualTo(100));
			Assert.That(repository.Current.Rank.TotalExperience, Is.EqualTo(120));
			Assert.That(repository.Current.Contracts.PendingSettlement, Is.Null);
		}

		private sealed class FailingFinalSaveStorage : ISaveStorage
		{
			private const int FinalSettlementWrite = 2;
			private readonly FileSaveStorage _inner;
			private int _writes;

			public FailingFinalSaveStorage(string root)
			{
				_inner = new FileSaveStorage(root);
			}

			public Awaitable<bool> ExistsAsync(string slotId, CancellationToken cancellationToken = default) => _inner.ExistsAsync(slotId, cancellationToken);
			public Awaitable<string> ReadAsync(string slotId, CancellationToken cancellationToken = default) => _inner.ReadAsync(slotId, cancellationToken);
			public Awaitable DeleteAsync(string slotId, CancellationToken cancellationToken = default) => _inner.DeleteAsync(slotId, cancellationToken);
			public Awaitable WriteAsync(string slotId, string payload, CancellationToken cancellationToken = default)
			{
				if (++_writes == FinalSettlementWrite)
				{
					throw new IOException("Injected final settlement save failure.");
				}
				return _inner.WriteAsync(slotId, payload, cancellationToken);
			}
		}

		[Test]
		public async Task Continue_RecoversPendingPaymentAndRank_AndSavesTheCompletedState()
		{
			CampaignRepository repository = NewRepository();
			repository.StartNew();
			ContractRun run = repository.Current.Contracts.Accept("first");
			run.CollectPackage();
			run.Depart();
			run.Deliver();
			repository.Current.PrepareSettlement();
			await repository.SaveAsync();
			CampaignRepository restored = NewRepository();
			await restored.ContinueAsync();
			Assert.That(restored.Current.Credits, Is.EqualTo(100));
			Assert.That(restored.Current.Rank.TotalExperience, Is.EqualTo(120));
			Assert.That(restored.Current.Contracts.PendingSettlement, Is.Null);
			await restored.ContinueAsync();
			Assert.That(restored.Current.Credits, Is.EqualTo(100));
			Assert.That(restored.Current.Rank.TotalExperience, Is.EqualTo(120));
		}

		[Test]
		public async Task FailedSettlementCheckpoint_DoesNotGrantMoneyOrRank_AndCanBeRetried()
		{
			File.WriteAllText(_root, "blocked");
			CampaignRepository repository = NewRepository();
			repository.StartNew();
			ContractRun run = repository.Current.Contracts.Accept("first");
			run.CollectPackage();
			run.Depart();
			run.Deliver();
			repository.Current.PrepareSettlement();
			Assert.That(await Capture(async () => { await repository.CompleteSettlementAsync(); }), Is.Not.Null);
			Assert.That(repository.Current.Credits, Is.Zero);
			Assert.That(repository.Current.Rank.TotalExperience, Is.Zero);
			File.Delete(_root);
			await repository.RecoverSettlementAsync();
			Assert.That(repository.Current.Credits, Is.EqualTo(100));
			Assert.That(repository.Current.Rank.TotalExperience, Is.EqualTo(120));
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
