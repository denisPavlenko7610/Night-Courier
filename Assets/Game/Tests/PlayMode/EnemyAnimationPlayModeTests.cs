#if UNITY_INCLUDE_TESTS
using System.Collections;
using NightCourier.Presentation;
using NUnit.Framework;
using RD.Health;
using RD.Mechanics.Combat;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NightCourier.PlayModeTests
{
	[Category("Integration")]
	public sealed class EnemyAnimationPlayModeTests
	{
		private const int TestTimeoutMilliseconds = 180000;
		private const string LightsOut = "contract.lights-out";
		private const string Express = "contract.express";
		private const string IdleState = "Idle";
		private const string HitState = "Hit";
		private const double TestDamage = 1d;

		private GameDriver _driver;

		[SetUp]
		public void DeleteTestSave()
		{
			GameDriver.DeleteTestSaves();

			_driver = new GameDriver();
		}

		[OneTimeTearDown]
		public void DeleteTestSaveAfterTheRun()
		{
			GameDriver.DeleteTestSaves();
		}

		[UnityTest, Timeout(TestTimeoutMilliseconds)]
		public IEnumerator Enemies_PlayIdle_AndReactToDamage()
		{
			yield return _driver.BootToMainMenu();
			yield return _driver.NewGameToDepot();
			yield return _driver.CompleteInState(GameDriver.StandardDelivery);
			yield return _driver.CompleteInState(GameDriver.Fragile);
			yield return _driver.CompleteInState(LightsOut);
			yield return _driver.AcceptInTerminal(Express);
			yield return _driver.CarryPackageToTheVanAndDepart();

			EnemyAnimation[] enemies = Object.FindObjectsByType<EnemyAnimation>(FindObjectsInactive.Exclude);

			Assert.That(enemies.Length, Is.GreaterThanOrEqualTo(2), "Express should field crawlers and a drone.");

			foreach (EnemyAnimation enemy in enemies)
			{
				Animator animator = GameDriver.Get<Animator>(enemy, "_animator");
				HealthCarrier health = GameDriver.Get<HealthCarrier>(enemy, "_health");

				yield return GameDriver.WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName(IdleState), enemy.name + " never reached Idle");

				health.ReceiveDamage(new DamageRequest(TestDamage, new DamageTypeId("impact"), "playmode-test", true));

				yield return GameDriver.WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName(HitState) || animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(HitState), enemy.name + " never played Hit");
			}
		}
	}
}
#endif
