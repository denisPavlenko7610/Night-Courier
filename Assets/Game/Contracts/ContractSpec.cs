using System;
using System.Collections.Generic;

namespace NightCourier.Contracts
{
	public sealed class ContractSpec
	{
		public ContractSpec(
			string id,
			string titleKey,
			string descriptionKey,
			ContractType type,
			string destinationSceneId,
			LocationState locationState,
			ContractRisk risk,
			int baseReward,
			double timeLimitSeconds,
			IEnumerable<string> prerequisites,
			bool isRepeatable,
			IEnumerable<ContractObjectiveSpec> objectives
		)
		{
			Id = Require(id, nameof(id));
			TitleKey = Require(titleKey, nameof(titleKey));
			DescriptionKey = Require(descriptionKey, nameof(descriptionKey));
			DestinationSceneId = Require(destinationSceneId, nameof(destinationSceneId));

			if (baseReward <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(baseReward), baseReward, "Base reward must be positive.");
			}

			if (timeLimitSeconds < 0d || double.IsNaN(timeLimitSeconds) || double.IsInfinity(timeLimitSeconds))
			{
				throw new ArgumentOutOfRangeException(
					nameof(timeLimitSeconds), timeLimitSeconds, "Time limit must be finite and not negative.");
			}

			Type = type;
			LocationState = locationState;
			Risk = risk;
			BaseReward = baseReward;
			TimeLimitSeconds = timeLimitSeconds;
			IsRepeatable = isRepeatable;
			Prerequisites = CopyPrerequisites(prerequisites);
			Objectives = CopyObjectives(objectives);
		}

		public string Id { get; }

		public string TitleKey { get; }

		public string DescriptionKey { get; }

		public ContractType Type { get; }

		public string DestinationSceneId { get; }

		public LocationState LocationState { get; }

		public ContractRisk Risk { get; }

		public int BaseReward { get; }

		public double TimeLimitSeconds { get; }

		public bool HasTimeLimit => TimeLimitSeconds > 0d;

		public bool IsRepeatable { get; }

		public IReadOnlyList<string> Prerequisites { get; }

		public IReadOnlyList<ContractObjectiveSpec> Objectives { get; }

		private static string Require(string value, string parameterName)
		{
			return string.IsNullOrWhiteSpace(value)
				? throw new ArgumentException("A value is required.", parameterName)
				: value;
		}

		private IReadOnlyList<string> CopyPrerequisites(IEnumerable<string> source)
		{
			List<string> result = new();
			HashSet<string> seen = new(StringComparer.Ordinal);

			foreach (string prerequisite in source ?? Array.Empty<string>())
			{
				if (string.IsNullOrWhiteSpace(prerequisite))
				{
					throw new ArgumentException("A prerequisite id cannot be empty.", nameof(source));
				}

				if (prerequisite == Id)
				{
					throw new ArgumentException($"Contract '{Id}' cannot require itself.", nameof(source));
				}

				if (!seen.Add(prerequisite))
				{
					throw new ArgumentException($"Contract '{Id}' repeats prerequisite '{prerequisite}'.", nameof(source));
				}

				result.Add(prerequisite);
			}

			return result.AsReadOnly();
		}

		private IReadOnlyList<ContractObjectiveSpec> CopyObjectives(IEnumerable<ContractObjectiveSpec> source)
		{
			List<ContractObjectiveSpec> result = new();
			HashSet<string> seen = new(StringComparer.Ordinal);

			foreach (ContractObjectiveSpec objective in source ?? Array.Empty<ContractObjectiveSpec>())
			{
				if (objective == null)
				{
					throw new ArgumentException("Objectives cannot contain null.", nameof(source));
				}

				if (!seen.Add(objective.Id))
				{
					throw new ArgumentException($"Contract '{Id}' repeats objective '{objective.Id}'.", nameof(source));
				}

				result.Add(objective);
			}

			return result.AsReadOnly();
		}
	}
}
