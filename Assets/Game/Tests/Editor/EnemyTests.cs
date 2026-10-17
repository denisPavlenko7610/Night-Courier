#if UNITY_INCLUDE_TESTS
using System;
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
