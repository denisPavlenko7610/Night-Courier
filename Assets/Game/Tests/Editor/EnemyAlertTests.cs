#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NightCourier.AI;
using NUnit.Framework;
using RD.Health;
using RD.Mechanics.ChaseTargetMemory;
using RD.Mechanics.Combat;
using UnityEditor;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class EnemyAlertTests
	{
		private GameObject _enemy;
		private GameObject _player;
		private HealthCarrier _health;
		private ChaseTargetMemory _memory;

		[SetUp]
		public void SetUp()
		{
			_player = new GameObject("Player");
			_enemy = new GameObject("Enemy");

			_health = _enemy.AddComponent<HealthCarrier>();
			_memory = _enemy.AddComponent<ChaseTargetMemory>();

			EnemyAlert alert = _enemy.AddComponent<EnemyAlert>();
			SerializedObject serialized = new(alert);

			serialized.FindProperty("_health").objectReferenceValue = _health;
			serialized.FindProperty("_memory").objectReferenceValue = _memory;
			serialized.ApplyModifiedPropertiesWithoutUndo();

			typeof(EnemyAlert).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(alert, null);
			_memory.Tick(0f);
			alert.Bind(_player.transform);
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_enemy);
			Object.DestroyImmediate(_player);
		}

		[Test]
		public void DirectionalDamage_MakesTheEnemyChaseTheAttacker()
		{
			Assert.That(_memory.CurrentState, Is.EqualTo(ChaseState.Idle));

			_health.ReceiveDamage(new DamageRequest(5d, default, "test"), Vector3.forward);

			Assert.That(_memory.CurrentState, Is.EqualTo(ChaseState.Chasing));
			Assert.That(_memory.Target, Is.EqualTo(_player.transform));
		}

		[Test]
		public void DamageWithoutADirection_IsIgnored()
		{
			_health.ReceiveDamage(new DamageRequest(5d, default, "floor"));

			Assert.That(_memory.CurrentState, Is.EqualTo(ChaseState.Idle));
		}

		[Test]
		public void LethalDamage_DoesNotStartAChase()
		{
			_health.ReceiveDamage(new DamageRequest(100000d, default, "test"), Vector3.forward);

			Assert.That(_memory.CurrentState, Is.EqualTo(ChaseState.Idle));
		}
	}
}
#endif
