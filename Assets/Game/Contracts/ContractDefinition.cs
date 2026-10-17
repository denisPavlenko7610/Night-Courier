using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightCourier.Contracts
{
	[CreateAssetMenu(fileName = "Contract", menuName = "Night Courier/Contract")]
	public sealed class ContractDefinition : ScriptableObject
	{
		[SerializeField] private string _id;
		[SerializeField] private string _titleKey;
		[SerializeField] private string _descriptionKey;
		[SerializeField] private ContractType _type = ContractType.Deliver;
		[SerializeField] private string _destinationSceneId = "warehouse";
		[SerializeField] private LocationState _locationState = LocationState.Normal;
		[SerializeField] private ContractRisk _risk = ContractRisk.Low;
		[SerializeField, Min(1)] private int _baseReward = 100;
		[SerializeField, Min(0f)] private float _timeLimitSeconds;
		[SerializeField] private bool _isRepeatable;
		[SerializeField] private string[] _prerequisites = Array.Empty<string>();
		[SerializeField] private ObjectiveEntry[] _objectives = Array.Empty<ObjectiveEntry>();
		[SerializeField] private PackageRuleKind[] _packageRules = Array.Empty<PackageRuleKind>();
		[SerializeField] private EscalationEntry[] _escalation = Array.Empty<EscalationEntry>();

		public string Id => _id;

		public ContractSpec ToSpec()
		{
			List<ContractObjectiveSpec> objectives = new(_objectives.Length);

			foreach (ObjectiveEntry entry in _objectives)
			{
				objectives.Add(new ContractObjectiveSpec(
					entry.Id, entry.LabelKey, entry.Kind, entry.ThresholdSeconds, entry.BonusCredits));
			}

			List<EscalationStep> escalation = new(_escalation.Length);

			foreach (EscalationEntry entry in _escalation)
			{
				escalation.Add(new EscalationStep(entry.AtViolation, entry.Effect));
			}

			return new ContractSpec(
				_id,
				_titleKey,
				_descriptionKey,
				_type,
				_destinationSceneId,
				_locationState,
				_risk,
				_baseReward,
				_timeLimitSeconds,
				_prerequisites,
				_isRepeatable,
				objectives,
				_packageRules,
				escalation);
		}

		[Serializable]
		private sealed class EscalationEntry
		{
			[SerializeField, Min(1)] private int _atViolation = 1;
			[SerializeField] private EscalationEffect _effect;

			public int AtViolation => _atViolation;

			public EscalationEffect Effect => _effect;
		}

		[Serializable]
		private sealed class ObjectiveEntry
		{
			[SerializeField] private string _id;
			[SerializeField] private string _labelKey;
			[SerializeField] private ContractObjectiveKind _kind;
			[SerializeField, Min(0f)] private float _thresholdSeconds;
			[SerializeField, Min(1)] private int _bonusCredits = 50;

			public string Id => _id;

			public string LabelKey => _labelKey;

			public ContractObjectiveKind Kind => _kind;

			public float ThresholdSeconds => _thresholdSeconds;

			public int BonusCredits => _bonusCredits;
		}
	}
}
