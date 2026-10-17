using System;
using System.Collections.Generic;
using System.Threading;
using NightCourier.Economy;
using RD.Quests;
using RD.Rewards;
using UnityEngine;

namespace NightCourier.Contracts
{
	public sealed class ContractBoard
	{
		public const string CreditsEntryId = "credits";

		private readonly ContractBook _book;
		private readonly QuestService _quests;
		private readonly IRewardGrantService _rewards;

		public ContractBoard(ContractBook book, QuestService quests, IRewardGrantService rewards)
		{
			_book = book ?? throw new ArgumentNullException(nameof(book));
			_quests = quests ?? throw new ArgumentNullException(nameof(quests));
			_rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
		}

		public ContractBook Book => _book;

		public ContractRun ActiveRun { get; private set; }

		public int CompletedCount
		{
			get
			{
				int count = 0;

				foreach (QuestState state in _quests.GetAll())
				{
					if (state.CompletionCount > 0)
					{
						count++;
					}
				}

				return count;
			}
		}

		public IReadOnlyList<ContractSpec> GetAvailable()
		{
			List<ContractSpec> available = new();

			if (ActiveRun != null)
			{
				return available;
			}

			foreach (ContractSpec spec in _book.All)
			{
				if (IsAvailable(spec))
				{
					available.Add(spec);
				}
			}

			return available;
		}

		public ContractRun Accept(string contractId)
		{
			if (ActiveRun != null)
			{
				throw new InvalidOperationException("A contract is already active.");
			}

			ContractSpec spec = _book.Get(contractId);

			if (!IsAvailable(spec) || !_quests.TryAccept(spec.Id))
			{
				throw new InvalidOperationException($"Contract '{contractId}' is not available.");
			}

			ActiveRun = new ContractRun(spec, _quests);
			return ActiveRun;
		}

		public bool Fail()
		{
			if (ActiveRun == null || !ActiveRun.Fail())
			{
				return false;
			}

			ActiveRun = null;
			return true;
		}

		public async Awaitable<ContractSettlement> SettleAsync(CancellationToken cancellationToken = default)
		{
			ContractRun run = ActiveRun;

			if (run == null || run.Phase != ContractPhase.Delivered)
			{
				throw new InvalidOperationException("There is no delivered contract to settle.");
			}

			ContractSettlement settlement = SettlementCalculator.Calculate(run.Spec, run.CaptureOutcome());

			if (settlement.Total > 0)
			{
				_quests.TryGet(run.Spec.Id, out QuestState state);

				RewardBundle bundle = new(
					new RewardBundleId($"payout.{run.Spec.Id}"),
					new IRewardEntry[]
					{
						new RewardEntry<long>(
							new RewardEntryId(CreditsEntryId),
							WalletCreditRewardHandler.RewardType,
							settlement.Total)
					});

				RewardGrantResult result = await _rewards.GrantAsync(
					new RewardGrantId($"payout.{run.Spec.Id}.{state.CompletionCount}"),
					bundle,
					cancellationToken);

				if (!result.IsSuccess)
				{
					throw new InvalidOperationException($"The reward was not granted: {result.Status}. {result.Error}");
				}
			}

			run.MarkCompleted();
			ActiveRun = null;

			return settlement;
		}

		public int AbandonUnfinished()
		{
			int abandoned = 0;

			foreach (QuestState state in _quests.GetAll())
			{
				if (state.Status == QuestStatus.Active && _quests.TryAbandon(state.Id))
				{
					abandoned++;
				}
			}

			ActiveRun = null;
			return abandoned;
		}

		private bool IsAvailable(ContractSpec spec)
		{
			if (!_quests.TryGet(spec.Id, out QuestState state))
			{
				return false;
			}

			return state.Status == QuestStatus.Available
				|| (state.Status == QuestStatus.Completed && spec.IsRepeatable);
		}
	}
}
