#if UNITY_INCLUDE_TESTS
using System;
using NightCourier.App;
using NightCourier.World;
using NUnit.Framework;
using RD.Conditions;
using RD.DependencyInjection;

namespace NightCourier.Tests
{
	public sealed class GameCompositionTests
	{
		private Container _root;

		[SetUp]
		public void SetUp()
		{
			ContainerBuilder builder = new();

			GameComposition.RegisterWarehouse(builder);

			_root = builder.Build();
		}

		[TearDown]
		public void TearDown()
		{
			_root.Dispose();
		}

		[Test]
		public void WarehouseServices_ArePerScene()
		{
			using Container first = _root.CreateScope();
			using Container second = _root.CreateScope();

			WarehouseFacts firstFacts = first.Resolve<WarehouseFacts>();
			firstFacts.SetPower(true);

			Assert.That(first.Resolve<WarehouseFacts>(), Is.SameAs(firstFacts));
			Assert.That(second.Resolve<WarehouseFacts>(), Is.Not.SameAs(firstFacts));
			Assert.That(second.Resolve<WarehouseFacts>().PowerOn, Is.False);
			Assert.That(first.Resolve<ConditionEngine>(), Is.Not.SameAs(second.Resolve<ConditionEngine>()));
		}

		[Test]
		public void WarehouseRules_EvaluateAgainstResolvedFacts()
		{
			using Container scene = _root.CreateScope();
			WarehouseFacts facts = scene.Resolve<WarehouseFacts>();
			ConditionEngine engine = scene.Resolve<ConditionEngine>();

			ConditionEvaluation locked = engine.Evaluate(WarehouseConditions.MaintenanceDoor(), WarehouseConditions.CreateContext(facts));

			facts.SetPower(true);

			ConditionEvaluation unlocked = engine.Evaluate(WarehouseConditions.MaintenanceDoor(), WarehouseConditions.CreateContext(facts));

			Assert.That(locked.IsSatisfied, Is.False);
			Assert.That(unlocked.IsSatisfied, Is.True);
		}

		[Test]
		public void DisposedScene_RejectsResolve()
		{
			Container scene = _root.CreateScope();

			scene.Dispose();

			Assert.That(() => scene.Resolve<WarehouseFacts>(), Throws.Exception);
		}

		[Test]
		public void Build_WithoutStartedServices_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => GameComposition.Build(null, null));
			Assert.Throws<InvalidOperationException>(() => GameComposition.Build(new GameServicesBuilder(), null));
		}

		[Test]
		public void RegisterWarehouse_WithoutBuilder_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => GameComposition.RegisterWarehouse(null));
		}
	}
}
#endif
