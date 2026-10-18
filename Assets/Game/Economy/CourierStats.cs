using System;
using RD.Stats;
using UnityEngine;

namespace NightCourier.Economy
{
	public sealed class CourierStats
	{
		public const string MaxHealthId = "courier.max_health";
		public const string MoveSpeedId = "courier.move_speed";
		public const string ReserveAmmoId = "courier.reserve_ammo";

		public const double BaseMaxHealth = 100d;
		public const double BaseMoveSpeed = 1d;
		public const double BaseReserveAmmo = 0d;

		private const double MinimumMoveSpeed = 0.5d;
		private const double MaximumMoveSpeed = 2d;
		private const string UpgradeSourcePrefix = "upgrade.";
		private const string UpgradeModifierId = "level";

		private readonly StatCollection _stats;

		public CourierStats()
		{
			_stats = new StatCollection(new[]
			{
				new StatDefinition(new StatId(MaxHealthId), BaseMaxHealth, StatClamp.AtLeast(1d)),
				new StatDefinition(new StatId(MoveSpeedId), BaseMoveSpeed, StatClamp.Between(MinimumMoveSpeed, MaximumMoveSpeed)),
				new StatDefinition(new StatId(ReserveAmmoId), BaseReserveAmmo, StatClamp.AtLeast(0d))
			});
		}

		public event Action Changed;

		public double MaxHealth => Value(MaxHealthId);

		public float MoveSpeedMultiplier => (float)Value(MoveSpeedId);

		public int ReserveAmmoBonus => Mathf.RoundToInt((float)Value(ReserveAmmoId));

		public bool Defines(string statId)
		{
			return _stats.TryGet(new StatId(statId), out _);
		}

		public void SetUpgradeLevel(UpgradeSpec spec, int level)
		{
			if (spec == null)
			{
				throw new ArgumentNullException(nameof(spec));
			}

			StatModifierSourceId source = new(UpgradeSourcePrefix + spec.Id);
			StatsResult removed = _stats.RemoveModifiersFromSource(source);

			if (removed.Status is not (StatsStatus.Success or StatsStatus.NoChanges))
			{
				throw new InvalidOperationException($"Upgrade '{spec.Id}' could not be cleared: {removed.Status}.");
			}

			if (level > 0)
			{
				StatsResult applied = _stats.UpsertModifier(
					new StatId(spec.StatId),
					new StatModifier(new StatModifierId(UpgradeModifierId), source, spec.Kind, spec.AmountPerLevel * level));

				if (applied.Status is not (StatsStatus.Success or StatsStatus.NoChanges))
				{
					throw new InvalidOperationException($"Upgrade '{spec.Id}' could not be applied: {applied.Status}.");
				}
			}

			Changed?.Invoke();
		}

		private double Value(string statId)
		{
			return _stats.TryGet(new StatId(statId), out StatState state)
				? state.Value
				: throw new InvalidOperationException($"Stat '{statId}' is not defined.");
		}
	}
}
