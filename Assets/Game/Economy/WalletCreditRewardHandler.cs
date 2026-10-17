using System;
using System.Threading;
using RD.Foundation;
using RD.Rewards;
using RD.Wallet;
using UnityEngine;

namespace NightCourier.Economy
{
	public sealed class WalletCreditRewardHandler : RewardHandler<long>
	{
		public static readonly RewardTypeId RewardType = new("wallet.credits");

		private readonly IWallet _wallet;

		public WalletCreditRewardHandler(IWallet wallet)
		{
			_wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
		}

		public override RewardTypeId Type => RewardType;

		protected override RewardValidationResult Validate(RewardEntry<long> entry)
		{
			return entry.Payload > 0
				? RewardValidationResult.Valid()
				: RewardValidationResult.Invalid("A credit reward must be positive.");
		}

		protected override Awaitable<RewardApplyResult> ApplyAsync(
			RewardGrantContext context,
			RewardEntry<long> entry,
			CancellationToken cancellationToken
		)
		{
			WalletOperationResult result = _wallet.ApplyGrant(
				new GrantId(context.EntryGrantKey),
				new[] { new CurrencyAmount(GameCurrencies.Credits, entry.Payload) }
			);

			RewardApplyResult mapped = result.Status switch
			{
				WalletOperationStatus.Committed => RewardApplyResult.Applied(),
				WalletOperationStatus.AlreadyProcessed => RewardApplyResult.AlreadyApplied(),
				_ => RewardApplyResult.Rejected(result.Message)
			};

			return AwaitableExtensions.FromResult(mapped);
		}
	}
}
