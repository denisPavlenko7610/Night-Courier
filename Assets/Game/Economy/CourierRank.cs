using System;
using NightCourier.Contracts;
using RD.Progression;

namespace NightCourier.Economy
{
	public readonly struct RankChange
	{
		public RankChange(long experienceGained, int previousLevel, int currentLevel)
		{
			ExperienceGained = experienceGained;
			PreviousLevel = previousLevel;
			CurrentLevel = currentLevel;
		}

		public long ExperienceGained { get; }

		public int PreviousLevel { get; }

		public int CurrentLevel { get; }

		public bool Promoted => CurrentLevel > PreviousLevel;
	}

	public sealed class CourierRank
	{
		public const string TrackId = "courier.rank";

		private static readonly long[] Thresholds = { 100, 300, 600 };

		private static readonly string[] TitleKeys =
		{
			"rank.trainee", "rank.courier", "rank.senior_courier", "rank.hazard_courier"
		};

		private readonly ProgressionService _progression;
		private readonly ProgressionId _track = new(TrackId);

		public CourierRank()
		{
			_progression = new ProgressionService(new[] { new ProgressionDefinition(_track, Thresholds) });
		}

		public int Level => State.Level;

		public bool IsMaxLevel => State.IsMaxLevel;

		public long TotalExperience => State.TotalExperience;

		public long ExperienceIntoLevel => State.ExperienceIntoLevel;

		public long ExperienceForNextLevel => State.ExperienceForNextLevel;

		public string TitleKey => TitleKeyOf(Level);

		private ProgressionState State
		{
			get
			{
				_progression.TryGet(_track, out ProgressionState state);

				return state;
			}
		}

		public static long ExperienceFor(ShiftGrade grade)
		{
			return grade switch
			{
				ShiftGrade.S => 120,
				ShiftGrade.A => 100,
				ShiftGrade.B => 70,
				ShiftGrade.C => 40,
				ShiftGrade.D => 15,
				_ => 0
			};
		}

		public static string TitleKeyOf(int level)
		{
			return TitleKeys[Math.Clamp(level, 1, TitleKeys.Length) - 1];
		}

		public RankChange Apply(ShiftRating rating)
		{
			int previous = Level;
			long gained = ExperienceFor(rating.Overall);

			if (gained > 0)
			{
				ProgressionResult result = _progression.AddExperience(_track, gained);

				if (!result.IsSuccess)
				{
					throw new InvalidOperationException($"The rank could not advance: {result.Status}. {result.Message}");
				}
			}

			return new RankChange(gained, previous, Level);
		}

		public ProgressionSnapshot Capture()
		{
			return _progression.CaptureSnapshot();
		}

		public void Restore(ProgressionSnapshot snapshot)
		{
			if (snapshot == null)
			{
				return;
			}

			ProgressionResult result = _progression.Restore(snapshot);

			if (!result.IsSuccess)
			{
				throw new InvalidOperationException($"The rank could not be restored: {result.Status}. {result.Message}");
			}
		}
	}
}
