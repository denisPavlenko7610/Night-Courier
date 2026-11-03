#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using NightCourier.Contracts;
using NightCourier.Packages;
using NightCourier.World;
using NUnit.Framework;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class LightFieldTests
	{
		private const float Range = 10f;

		private readonly List<GameObject> _created = new();

		[TearDown]
		public void TearDown()
		{
			foreach (GameObject created in _created)
			{
				if (created != null)
				{
					Object.DestroyImmediate(created);
				}
			}

			_created.Clear();
		}

		private static LightSource Source(LightType type, Vector3 forward, float spotAngle = 60f, bool shadows = true)
		{
			return new LightSource(type, Vector3.zero, forward, Range, spotAngle, shadows);
		}

		private static bool Open(Vector3 from, Vector3 to)
		{
			return false;
		}

		private static bool Walled(Vector3 from, Vector3 to)
		{
			return true;
		}

		[Test]
		public void Point_LightsInsideRangeAndNotOutside()
		{
			LightSource point = Source(LightType.Point, Vector3.forward);

			Assert.That(LightReach.Reaches(point, new Vector3(Range - 0.1f, 0f, 0f), Open), Is.True);
			Assert.That(LightReach.Reaches(point, new Vector3(Range + 0.1f, 0f, 0f), Open), Is.False);
		}

		[Test]
		public void Point_BehindAWall_IsDarkOnlyWhenTheLightCastsShadows()
		{
			Vector3 behindWall = new(5f, 0f, 0f);

			Assert.That(LightReach.Reaches(Source(LightType.Point, Vector3.forward, shadows: true), behindWall, Walled), Is.False);
			Assert.That(LightReach.Reaches(Source(LightType.Point, Vector3.forward, shadows: false), behindWall, Walled), Is.True);
		}

		[Test]
		public void Spot_LightsOnlyInsideItsCone()
		{
			LightSource spot = Source(LightType.Spot, Vector3.right, spotAngle: 60f);

			Assert.That(LightReach.Reaches(spot, new Vector3(5f, 0f, 0f), Open), Is.True);
			Assert.That(LightReach.Reaches(spot, new Vector3(5f, 0f, 2f), Open), Is.True);
			Assert.That(LightReach.Reaches(spot, new Vector3(5f, 0f, 4f), Open), Is.False);
			Assert.That(LightReach.Reaches(spot, new Vector3(0f, 0f, 5f), Open), Is.False);
			Assert.That(LightReach.Reaches(spot, new Vector3(-5f, 0f, 0f), Open), Is.False);
			Assert.That(LightReach.Reaches(spot, new Vector3(Range + 1f, 0f, 0f), Open), Is.False);
		}

		[Test]
		public void Directional_LightsEverywhereUnlessShadowedFromAbove()
		{
			Vector3 down = Vector3.down;
			Vector3 point = new(100f, 0f, 100f);

			Assert.That(LightReach.Reaches(Source(LightType.Directional, down), point, Open), Is.True);
			Assert.That(LightReach.Reaches(Source(LightType.Directional, down, shadows: true), point, Walled), Is.False);
			Assert.That(LightReach.Reaches(Source(LightType.Directional, down, shadows: false), point, Walled), Is.True);
		}

		[Test]
		public void AreaLights_DoNotCountAsLightSources()
		{
			Assert.That(LightReach.Reaches(Source(LightType.Rectangle, Vector3.forward), Vector3.zero, Open), Is.False);
		}

		[Test]
		public void Reaches_RequiresAnOcclusionTest()
		{
			Assert.Throws<System.ArgumentNullException>(
				() => LightReach.Reaches(Source(LightType.Point, Vector3.forward), Vector3.zero, null));
		}

		private WarehouseLighting CreateLighting(params Light[] lights)
		{
			GameObject root = new("Lighting");
			_created.Add(root);

			WarehouseLighting lighting = root.AddComponent<WarehouseLighting>();
			typeof(WarehouseLighting)
				.GetField("_lights", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(lighting, lights);

			return lighting;
		}

		private Light CreateLight(LightType type, Vector3 position, Vector3 forward, LightShadows shadows)
		{
			GameObject holder = new("Light");
			_created.Add(holder);
			holder.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));

			Light light = holder.AddComponent<Light>();
			light.type = type;
			light.range = Range;
			light.spotAngle = 60f;
			light.intensity = 1f;
			light.shadows = shadows;

			return light;
		}

		private void CreateWall(Vector3 position)
		{
			GameObject wall = new("Wall");
			_created.Add(wall);
			wall.transform.position = position;
			wall.AddComponent<BoxCollider>().size = new Vector3(0.2f, 4f, 10f);

			Physics.SyncTransforms();
		}

		[Test]
		public void Warehouse_PointLightWithShadows_RespectsRadiusAndWalls()
		{
			Light light = CreateLight(LightType.Point, new Vector3(0f, 2f, 0f), Vector3.forward, LightShadows.Soft);
			WarehouseLighting lighting = CreateLighting(light);

			CreateWall(new Vector3(5f, 2f, 0f));

			Assert.That(lighting.IsLit(new Vector3(3f, 1f, 0f)), Is.True, "inside the light, same side of the wall");
			Assert.That(lighting.IsLit(new Vector3(8f, 1f, 0f)), Is.False, "inside the radius but behind the wall");
			Assert.That(lighting.IsLit(new Vector3(-3f, 1f, 6f)), Is.True, "inside the radius on the open side");
			Assert.That(lighting.IsLit(new Vector3(-12f, 1f, 0f)), Is.False, "outside the radius");
		}

		[Test]
		public void Warehouse_PointLightWithoutShadows_LightsThroughWallsLikeTheRenderer()
		{
			Light light = CreateLight(LightType.Point, new Vector3(0f, 2f, 0f), Vector3.forward, LightShadows.None);
			WarehouseLighting lighting = CreateLighting(light);

			CreateWall(new Vector3(5f, 2f, 0f));

			Assert.That(lighting.IsLit(new Vector3(8f, 1f, 0f)), Is.True);
		}

		[Test]
		public void Warehouse_Blackout_IsDarkUntilPowerIsRestored()
		{
			Light light = CreateLight(LightType.Point, new Vector3(0f, 2f, 0f), Vector3.forward, LightShadows.None);
			WarehouseLighting lighting = CreateLighting(light);
			WarehouseFacts facts = new();
			Vector3 point = new(2f, 1f, 0f);

			lighting.Bind(LocationState.Blackout, facts);

			Assert.That(lighting.IsLit(point), Is.False);

			facts.SetPower(true);

			Assert.That(lighting.IsLit(point), Is.True);

			facts.SetPower(false);

			Assert.That(lighting.IsLit(point), Is.False);
		}

		[Test]
		public void Warehouse_EmergencyLamp_LightsItsPoolDuringTheBlackout_AndWallsStillBlockIt()
		{
			Light hall = CreateLight(LightType.Point, new Vector3(0f, 5f, 0f), Vector3.forward, LightShadows.None);
			Light lamp = CreateLight(LightType.Point, new Vector3(40f, 2f, 0f), Vector3.forward, LightShadows.Soft);
			WarehouseLighting lighting = CreateLighting(hall);
			WarehouseFacts facts = new();

			typeof(WarehouseLighting)
				.GetField("_emergencyLights", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(lighting, new[] { lamp });
			CreateWall(new Vector3(45f, 2f, 0f));
			lighting.Bind(LocationState.Blackout, facts);

			Vector3 inPool = new(42f, 1f, 0f);
			Vector3 behindWall = new(48f, 1f, 0f);
			Vector3 hallFloor = new(2f, 1f, 0f);

			Assert.That(lighting.IsLit(inPool), Is.True, "the lamp stays on without power");
			Assert.That(lighting.IsLit(behindWall), Is.False, "the wall blocks the lamp");
			Assert.That(lighting.IsLit(hallFloor), Is.False, "the hall lights need power");

			facts.SetPower(true);

			Assert.That(lighting.IsLit(hallFloor), Is.True);
			Assert.That(lighting.IsLit(inPool), Is.True);
		}

		[Test]
		public void Warehouse_NormalState_IsLitWithoutThePowerFact()
		{
			Light light = CreateLight(LightType.Point, new Vector3(0f, 2f, 0f), Vector3.forward, LightShadows.None);
			WarehouseLighting lighting = CreateLighting(light);

			lighting.Bind(LocationState.Normal, new WarehouseFacts());

			Assert.That(lighting.IsLit(new Vector3(2f, 1f, 0f)), Is.True);
		}

		[Test]
		public void Warehouse_DisabledOrDimLights_DoNotLight()
		{
			Light off = CreateLight(LightType.Point, new Vector3(0f, 2f, 0f), Vector3.forward, LightShadows.None);
			Light dim = CreateLight(LightType.Point, new Vector3(0f, 2f, 0f), Vector3.forward, LightShadows.None);
			WarehouseLighting lighting = CreateLighting(off, dim);

			off.enabled = false;
			dim.intensity = 0f;

			Assert.That(lighting.IsLit(new Vector3(2f, 1f, 0f)), Is.False);
		}
	}
}
#endif
