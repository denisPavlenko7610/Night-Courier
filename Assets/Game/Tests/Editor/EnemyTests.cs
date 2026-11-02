#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using NightCourier.AI;
using NightCourier.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class EnemyTests
	{
		[Test]
		public void StandoffPoint_PlacesEnemyOnItsSideOfTheTargetAtTheDistance()
		{
			Vector3 point = EnemyMath.StandoffPoint(new Vector3(10f, 0.5f, 0f), new Vector3(0f, 0f, 0f), 6f);

			Assert.That(point.x, Is.EqualTo(6f).Within(0.001f));
			Assert.That(point.z, Is.EqualTo(0f).Within(0.001f));
			Assert.That(point.y, Is.EqualTo(0.5f).Within(0.001f));
		}

		[Test]
		public void StandoffPoint_OnTopOfTheTarget_StaysPut()
		{
			Vector3 enemy = new(1f, 0f, 1f);

			Assert.That(EnemyMath.StandoffPoint(enemy, new Vector3(1f, 5f, 1f), 6f), Is.EqualTo(enemy));
		}

		[Test]
		public void PlanarDistance_IgnoresHeight()
		{
			Assert.That(EnemyMath.PlanarDistance(new Vector3(0f, 0f, 0f), new Vector3(3f, 9f, 4f)), Is.EqualTo(5f).Within(0.001f));
		}

		private static Quaternion PreviousBrainTurn(Quaternion current, Vector3 direction, float maxDegrees)
		{
			direction.y = 0f;

			if (direction.sqrMagnitude < 0.001f)
			{
				return current;
			}

			return Quaternion.RotateTowards(current, Quaternion.LookRotation(direction), maxDegrees);
		}

		private static IEnumerable<TestCaseData> TurnCases()
		{
			Vector3[] directions =
			{
				Vector3.zero, new(0f, 3f, 0f), new(0.01f, 0f, 0f), new(0.03f, 0f, 0.01f),
				new(1f, 0f, 0f), new(1f, 5f, 0f), new(-1f, 0f, -1f), new(0f, 0f, -2f)
			};
			float[] steps = { 0f, 5f, 90f, 360f };

			foreach (Vector3 direction in directions)
			{
				foreach (float step in steps)
				{
					yield return new TestCaseData(direction, step);
				}
			}
		}

		[TestCaseSource(nameof(TurnCases))]
		public void FaceToward_MatchesThePreviousBrainTurn(Vector3 direction, float maxDegrees)
		{
			GameObject body = new("PH_Enemy");

			try
			{
				body.transform.rotation = Quaternion.Euler(0f, 37f, 0f);

				Quaternion expected = PreviousBrainTurn(body.transform.rotation, direction, maxDegrees);

				EnemyMath.FaceToward(body.transform, direction, maxDegrees);

				Assert.That(Quaternion.Angle(body.transform.rotation, expected), Is.LessThan(0.001f));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(body);
			}
		}

		[Test]
		public void FaceToward_TurnsByAtMostTheStep_AndReachesTheDirectionWithAFullStep()
		{
			GameObject body = new("PH_Enemy");

			try
			{
				EnemyMath.FaceToward(body.transform, Vector3.right, 30f);

				Assert.That(Quaternion.Angle(Quaternion.identity, body.transform.rotation), Is.EqualTo(30f).Within(0.01f));

				EnemyMath.FaceToward(body.transform, new Vector3(1f, 5f, 0f), 360f);

				Assert.That(Vector3.Angle(body.transform.forward, Vector3.right), Is.LessThan(0.01f));
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(body);
			}
		}

		[Test]
		public void ShouldFire_NeedsRangeAndLineOfSight()
		{
			Assert.That(EnemyMath.ShouldFire(10f, 14f, true), Is.True);
			Assert.That(EnemyMath.ShouldFire(15f, 14f, true), Is.False);
			Assert.That(EnemyMath.ShouldFire(10f, 14f, false), Is.False);
		}

		[Test]
		public void Spec_RepeatedEncounter_Throws()
		{
			Assert.Throws<ArgumentException>(() => new ContractSpec(
				"x", "x.t", "x.d", ContractType.Deliver, "warehouse", LocationState.Normal, ContractRisk.Low, 100, 0d,
				Array.Empty<string>(), false, Array.Empty<ContractObjectiveSpec>(), null, null,
				new[] { EncounterKind.Crawlers, EncounterKind.Crawlers }));
		}

		[Test]
		public void Spec_KeepsEncounters()
		{
			ContractSpec spec = new(
				"x", "x.t", "x.d", ContractType.Deliver, "warehouse", LocationState.Normal, ContractRisk.Low, 100, 0d,
				Array.Empty<string>(), false, Array.Empty<ContractObjectiveSpec>(), null, null,
				new[] { EncounterKind.SecurityDrone, EncounterKind.Crawlers });

			Assert.That(spec.Encounters, Is.EqualTo(new[] { EncounterKind.SecurityDrone, EncounterKind.Crawlers }));
			Assert.That(ContractFixtures.Spec("plain").Encounters, Is.Empty);
		}
	}
}
#endif
