#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NightCourier.Presentation;
using UnityEngine;

namespace NightCourier.PlayModeTests
{
	internal sealed class EnemyStateLog
	{
		private static readonly string[] StateNames = { "Idle", "Walk", "Attack", "Hit", "TurnOff" };

		private readonly Animator _animator;
		private readonly HashSet<string> _states = new();
		private float _minDistance = float.MaxValue;

		public EnemyStateLog(EnemyAnimation enemy)
		{
			Enemy = enemy;
			_animator = GameDriver.Get<Animator>(enemy, "_animator");
		}

		public EnemyAnimation Enemy { get; }

		public bool Saw(string state)
		{
			return _states.Contains(state);
		}

		public void Sample(Vector3 player)
		{
			if (Enemy == null)
			{
				return;
			}

			Vector3 offset = Enemy.transform.position - player;
			offset.y = 0f;
			_minDistance = Mathf.Min(_minDistance, offset.magnitude);

			AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);

			foreach (string name in StateNames)
			{
				if (info.IsName(name))
				{
					_states.Add(name);
				}
			}

			if (_animator.IsInTransition(0))
			{
				AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);

				foreach (string name in StateNames)
				{
					if (next.IsName(name))
					{
						_states.Add(name);
					}
				}
			}
		}

		public string Describe()
		{
			string name = Enemy != null ? Enemy.name : "(destroyed)";

			return $"   enemy {name}: closest {_minDistance:F1} m, states seen [{string.Join(",", _states)}]";
		}
	}
}
#endif
