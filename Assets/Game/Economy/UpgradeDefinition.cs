using System;
using RD.Stats;
using UnityEngine;

namespace NightCourier.Economy
{
	[CreateAssetMenu(fileName = "Upgrade", menuName = "Night Courier/Upgrade")]
	public sealed class UpgradeDefinition : ScriptableObject
	{
		[SerializeField] private string _id;
		[SerializeField] private string _titleKey;
		[SerializeField] private string _descriptionKey;
		[SerializeField] private string _statId;
		[SerializeField] private StatModifierKind _kind = StatModifierKind.Flat;
		[SerializeField, Min(0.001f)] private float _amountPerLevel = 1f;
		[SerializeField] private long[] _prices = Array.Empty<long>();

		public UpgradeSpec ToSpec()
		{
			return new UpgradeSpec(_id, _titleKey, _descriptionKey, _statId, _kind, _amountPerLevel, _prices);
		}
	}
}
