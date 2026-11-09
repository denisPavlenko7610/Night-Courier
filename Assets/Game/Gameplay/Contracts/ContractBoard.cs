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

		public PendingSettlement PendingSettlement { get; private set; }

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

			if (ActiveRun != null || PendingSettlement != null)
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
			if (ActiveRun != null || PendingSettlement != null)
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

			if (PendingSettlement != null || run == null || run.Phase != ContractPhase.Delivered)
			{
				throw new InvalidOperationException("There is no delivered contract to settle.");
			}

			ContractSettlement settlement = SettlementCalculator.Calculate(run.Spec, run.CaptureOutcome());

			_quests.TryGet(run.Spec.Id, out QuestState state);
			await GrantSettlementAsync(
				settlement.ContractId,
				settlement.Total,
				state.CompletionCount,
				cancellationToken
			);

			run.MarkCompleted();
			ActiveRun = null;

			return settlement;
		}


		internal ContractSettlement PrepareSettlement(long rankBefore)
		{
			if (PendingSettlement != null)
			{
				throw new InvalidOperationException("A settlement is already pending.");
			}
			ContractRun run = ActiveRun;
			if (run == null || run.Phase != ContractPhase.Delivered)
			{
				throw new InvalidOperationException("There is no delivered contract to settle.");
			}
			ContractSettlement settlement = SettlementCalculator.Calculate(run.Spec, run.CaptureOutcome());
			_quests.TryGet(run.Spec.Id, out QuestState state);
			PendingSettlement = new PendingSettlement
			{
				ContractId = run.Spec.Id,
				CompletionCount = state.CompletionCount,
				Total = settlement.Total,
				DeliveryGrade =
					settlement.Rating.DeliveryTime.HasValue ? (int)settlement.Rating.DeliveryTime.Value : -1,
				CargoGrade = settlement.Rating.CargoCondition,
				ProtocolGrade = settlement.Rating.ProtocolCompliance,
				PropertyGrade = settlement.Rating.PropertyDamage,
				RankBefore = rankBefore
			};
			return settlement;
		}

		internal void RestoreSettlement(PendingSettlement pending)
		{
			if (pending == null)
			{
				return;
			}
			pending.Validate();
			if (!_quests.TryGet(pending.ContractId, out QuestState state) ||
				state.Status != QuestStatus.Completed ||
				state.CompletionCount != pending.CompletionCount)
			{
				throw new RD.Save.InvalidSaveDataException(
					"The pending settlement does not match the completed contract."
				);
			}
			PendingSettlement = pending.Copy();
		}

		internal async Awaitable PayPendingSettlementAsync(CancellationToken cancellationToken)
		{
			PendingSettlement pending = PendingSettlement ??
				throw new InvalidOperationException("There is no pending settlement.");
			await GrantSettlementAsync(pending.ContractId, pending.Total, pending.CompletionCount, cancellationToken);
			if (ActiveRun != null)
			{
				ActiveRun.MarkCompleted();
				ActiveRun = null;
			}
		}

		internal void ClearSettlement()
		{
			PendingSettlement = null;
		}

		private async Awaitable GrantSettlementAsync(
			string contractId,
			long total,
			int completionCount,
			CancellationToken cancellationToken
		)
		{
			if (total > 0)
			{
				RewardBundle bundle = new(
					new RewardBundleId($"payout.{contractId}"),
					new IRewardEntry[]
					{
						new RewardEntry<long>(
							new RewardEntryId(CreditsEntryId),
							WalletCreditRewardHandler.RewardType,
							total
						)
					}
				);

				RewardGrantResult result = await _rewards.GrantAsync(
					new RewardGrantId($"payout.{contractId}.{completionCount}"),
					bundle,
					cancellationToken
				);

				if (!result.IsSuccess)
				{
					throw new InvalidOperationException($"The reward was not granted: {result.Status}. {result.Error}");
				}
			}
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

			return state.Status == QuestStatus.Available ||
				(state.Status == QuestStatus.Completed && spec.IsRepeatable);
		}
	}
}