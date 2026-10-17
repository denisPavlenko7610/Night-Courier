using System;
using RD.Conditions;
using RD.Mechanics.ConditionLock;
using RD.Mechanics.Door;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class ConditionDoor : MonoBehaviour
	{
		[SerializeField] private ConditionLock _lock;
		[SerializeField] private Door _door;

		private WarehouseFacts _facts;
		private ConditionContext _context;

		public bool IsUnlocked => _lock.IsUnlocked;

		public bool IsOpen => _door.IsOpen;

		public void Bind(WarehouseFacts facts, ConditionEngine engine, ConditionRule rule)
		{
			Unbind();

			_facts = facts ?? throw new ArgumentNullException(nameof(facts));
			_context = WarehouseConditions.CreateContext(facts);

			_lock.Configure(engine, rule);
			_lock.Unlocked += OnUnlocked;
			_facts.Changed += Evaluate;

			Evaluate();
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_facts != null)
			{
				_facts.Changed -= Evaluate;
				_facts = null;
			}

			if (_lock != null)
			{
				_lock.Unlocked -= OnUnlocked;
			}
		}

		private void Evaluate()
		{
			_lock.TryUnlock(_context, out _);
		}

		private void OnUnlocked(ConditionLock unlocked)
		{
			_door.Open();
		}
	}
}
