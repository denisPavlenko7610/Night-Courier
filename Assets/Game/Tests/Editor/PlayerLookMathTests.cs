#if UNITY_INCLUDE_TESTS
using NightCourier.Player;
using NUnit.Framework;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class PlayerLookMathTests
	{
		[Test]
		public void ClampPitch_StaysWithinLimit()
		{
			Assert.That(PlayerLookMath.ClampPitch(0f, 30f, 85f), Is.EqualTo(30f));
			Assert.That(PlayerLookMath.ClampPitch(80f, 30f, 85f), Is.EqualTo(85f));
			Assert.That(PlayerLookMath.ClampPitch(-80f, -30f, 85f), Is.EqualTo(-85f));
		}

		[Test]
		public void PlanarVelocity_FollowsYawAndSpeed()
		{
			Vector3 forward = PlayerLookMath.PlanarVelocity(new Vector2(0f, 1f), Quaternion.identity, 4f);
			Vector3 turned = PlayerLookMath.PlanarVelocity(new Vector2(0f, 1f), Quaternion.Euler(0f, 90f, 0f), 4f);

			Assert.That(forward.z, Is.EqualTo(4f).Within(0.001f));
			Assert.That(turned.x, Is.EqualTo(4f).Within(0.001f));
			Assert.That(turned.y, Is.EqualTo(0f));
		}

		[Test]
		public void PlanarVelocity_DiagonalIsNotFasterThanStraight()
		{
			Vector3 diagonal = PlayerLookMath.PlanarVelocity(new Vector2(1f, 1f), Quaternion.identity, 5f);

			Assert.That(diagonal.magnitude, Is.EqualTo(5f).Within(0.001f));
		}

		[Test]
		public void PlanarVelocity_NoInputMeansNoMovement()
		{
			Assert.That(PlayerLookMath.PlanarVelocity(Vector2.zero, Quaternion.identity, 5f), Is.EqualTo(Vector3.zero));
		}
	}
}
#endif
