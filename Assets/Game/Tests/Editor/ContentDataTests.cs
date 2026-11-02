#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.IO;
using NightCourier.App;
using NightCourier.Contracts;
using NightCourier.Economy;
using NightCourier.Packages;
using NightCourier.Weapons;
using NUnit.Framework;
using RD.SceneFlow;
using UnityEditor;
using UnityEngine;

namespace NightCourier.Tests
{
	public sealed class ContentDataTests
	{
		private const string ContractCatalogPath = "Assets/Game/Content/Contracts/ContractCatalog.asset";
		private const string UpgradeCatalogPath = "Assets/Game/Content/Upgrades/UpgradeCatalog.asset";
		private const string SceneCatalogPath = "Assets/Game/Content/SceneCatalog.asset";
		private const string LocalizationPath = "Assets/Game/Content/Localization/en.json";

		private const string StandardDelivery = "contract.standard-delivery";
		private const string Fragile = "contract.fragile";
		private const string LightsOut = "contract.lights-out";
		private const string Express = "contract.express";
		private const string RestrictedCargo = "contract.restricted-cargo";

		private ContractBook _book;

		[SetUp]
		public void SetUp()
		{
			_book = Load<ContractCatalog>(ContractCatalogPath).BuildBook();
		}

		[Test]
		public void ContractCatalog_BuildsTheFiveMvpContracts()
		{
			string[] expected = { StandardDelivery, Fragile, LightsOut, Express, RestrictedCargo };
			List<string> actual = new();

			foreach (ContractSpec spec in _book.All)
			{
				actual.Add(spec.Id);
			}

			Assert.That(actual, Is.EquivalentTo(expected));
		}

		[TestCase(StandardDelivery, new PackageRuleKind[0], LocationState.Normal, false)]
		[TestCase(Fragile, new[] { PackageRuleKind.DoNotDrop }, LocationState.Normal, false)]
		[TestCase(LightsOut, new[] { PackageRuleKind.KeepInLight }, LocationState.Blackout, false)]
		[TestCase(Express, new[] { PackageRuleKind.DeliveryDeadline }, LocationState.Normal, true)]
		[TestCase(RestrictedCargo, new[] { PackageRuleKind.DoNotDrop, PackageRuleKind.KeepInLight }, LocationState.Blackout, false)]
		public void Contract_FollowsTheDocumentedMvpTable(
			string id,
			PackageRuleKind[] rules,
			LocationState state,
			bool hasTimeLimit)
		{
			ContractSpec spec = _book.Get(id);

			Assert.That(spec.Type, Is.EqualTo(ContractType.Deliver));
			Assert.That(spec.LocationState, Is.EqualTo(state));
			Assert.That(spec.PackageRules, Is.EquivalentTo(rules));
			Assert.That(spec.HasTimeLimit, Is.EqualTo(hasTimeLimit));
		}

		[Test]
		public void Contract_EncountersFollowTheDocumentedEnemyRoster()
		{
			Assert.That(_book.Get(LightsOut).Encounters, Does.Contain(EncounterKind.Crawlers));
			Assert.That(_book.Get(Express).Encounters, Does.Contain(EncounterKind.SecurityDrone));
			Assert.That(_book.Get(RestrictedCargo).Encounters, Is.EquivalentTo(new[] { EncounterKind.Crawlers, EncounterKind.SecurityDrone }));
		}

		[TestCase(Fragile)]
		[TestCase(LightsOut)]
		[TestCase(RestrictedCargo)]
		public void RuleContracts_UseTheDefaultViolationLadder(string id)
		{
			ContractSpec spec = _book.Get(id);

			Assert.That(spec.GetEscalation(1), Is.EqualTo(new[] { EscalationEffect.PackageUnstable }));
			Assert.That(spec.GetEscalation(2), Is.EqualTo(new[] { EscalationEffect.SpawnEnemy }));
			Assert.That(spec.GetEscalation(3), Is.EqualTo(new[] { EscalationEffect.PackageHazardous }));
			Assert.That(spec.GetEscalation(4), Is.EqualTo(new[] { EscalationEffect.FailContract }));
			Assert.That(spec.GetEscalation(5), Is.Empty);
		}

		[Test]
		public void TutorialContract_HasNoEscalationAndNoPrerequisites()
		{
			ContractSpec tutorial = _book.Get(StandardDelivery);

			Assert.That(tutorial.Escalation, Is.Empty);
			Assert.That(tutorial.Prerequisites, Is.Empty);
		}

		[Test]
		public void EveryContract_BecomesAvailableOnceItsPrerequisitesAreCompleted()
		{
			HashSet<string> completed = new();
			bool progressed = true;

			while (progressed)
			{
				progressed = false;

				foreach (ContractSpec spec in _book.All)
				{
					if (completed.Contains(spec.Id) || !completed.IsSupersetOf(spec.Prerequisites))
					{
						continue;
					}

					completed.Add(spec.Id);
					progressed = true;
				}
			}

			Assert.That(completed.Count, Is.EqualTo(_book.All.Count), "Some contracts can never be unlocked.");
		}

		[Test]
		public void EveryContract_PaysAPositiveRewardAndLabelsItsBonuses()
		{
			foreach (ContractSpec spec in _book.All)
			{
				Assert.That(spec.BaseReward, Is.GreaterThan(0), spec.Id);

				foreach (ContractObjectiveSpec objective in spec.Objectives)
				{
					Assert.That(objective.BonusCredits, Is.GreaterThan(0), spec.Id + "/" + objective.Id);
				}
			}
		}

		[Test]
		public void DestinationScenes_AreRegisteredAndEnabledInTheBuild()
		{
			SceneCatalog scenes = Load<SceneCatalog>(SceneCatalogPath);
			HashSet<string> enabledInBuild = new();

			foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
			{
				if (scene.enabled)
				{
					enabledInBuild.Add(scene.path);
				}
			}

			HashSet<string> ids = new() { SceneIds.Boot, SceneIds.MainMenu, SceneIds.Depot, SceneIds.Warehouse };

			foreach (ContractSpec spec in _book.All)
			{
				ids.Add(spec.DestinationSceneId);
			}

			foreach (string id in ids)
			{
				string path = scenes.GetPath(id);

				Assert.That(File.Exists(path), Is.True, $"Scene '{id}' points to a missing file: {path}");
				Assert.That(enabledInBuild, Does.Contain(path), $"Scene '{id}' is not enabled in Build Settings.");
			}
		}

		[Test]
		public void UpgradeCatalog_TargetsKnownStatsAndCoversTheMvpUpgrades()
		{
			UpgradeBook upgrades = Load<UpgradeCatalog>(UpgradeCatalogPath).BuildBook();
			CourierStats stats = new();
			HashSet<string> targeted = new();

			foreach (UpgradeSpec spec in upgrades.All)
			{
				Assert.That(stats.Defines(spec.StatId), Is.True, spec.Id);
				Assert.That(spec.PriceOfNextLevel(0), Is.GreaterThan(0), spec.Id);
				targeted.Add(spec.StatId);
			}

			Assert.That(targeted, Does.Contain(CourierStats.MaxHealthId));
			Assert.That(targeted, Does.Contain(CourierStats.MoveSpeedId));
		}

		[Test]
		public void Localization_ContainsEveryKeyTheContentReferences()
		{
			JsonLocalizationProvider table = new(Load<TextAsset>(LocalizationPath));
			List<string> missing = new();

			void Require(string key)
			{
				if (!table.TryGet(key, null, out _))
				{
					missing.Add(key);
				}
			}

			foreach (ContractSpec spec in _book.All)
			{
				Require(spec.TitleKey);
				Require(spec.DescriptionKey);

				foreach (ContractObjectiveSpec objective in spec.Objectives)
				{
					Require(objective.LabelKey);
				}

				foreach (PackageRuleKind rule in spec.PackageRules)
				{
					Require(PackageRuleKeys.NameKey(rule));
					Require(PackageRuleKeys.HintKey(rule));
					Require(PackageRuleKeys.AdviceKey(rule));
				}

				Require(ContractKeys.RiskKey(spec.Risk));
				Require(ContractKeys.StateKey(spec.LocationState));

				foreach (EncounterKind encounter in spec.Encounters)
				{
					Require(ContractKeys.ThreatKey(encounter));
				}
			}

			foreach (UpgradeSpec spec in Load<UpgradeCatalog>(UpgradeCatalogPath).BuildBook().All)
			{
				Require(spec.TitleKey);
				Require(spec.DescriptionKey);
			}

			foreach (WeaponKind kind in new[] { WeaponKind.StunPistol, WeaponKind.Crowbar })
			{
				Require(WeaponRules.NameKey(kind));
			}

			for (int level = 1; level <= 4; level++)
			{
				Require(CourierRank.TitleKeyOf(level));
			}

			Require(SettlementCalculator.BasePayLabelKey);
			Require(SettlementCalculator.AmmoExpenseLabelKey);
			Require(SettlementCalculator.PropertyDamageLabelKey);
			Require(SettlementCalculator.DeathPenaltyLabelKey);

			string[] hudKeys =
			{
				"contract.briefing", "contract.rules", "contract.none", "contract.time_limit", "contract.bonus", "contract.threats",
				"notice.scan_first", "result.stats", "result.save_failed", "result.settlement_failed",
				ObjectiveHints.RestorePower, ObjectiveHints.ScanPackage, ObjectiveHints.Scanning, ObjectiveHints.PickUpScanned,
				ObjectiveHints.DeliverPackage, ObjectiveHints.PickUpPackage, ObjectiveHints.FindBattery, ObjectiveHints.InsertBattery,
				ObjectiveHints.AcceptContract, ObjectiveHints.PickUpFromConveyor, ObjectiveHints.LoadVan,
				SettlementAdvice.CleanKey, SettlementAdvice.OverdueKey, SettlementAdvice.CargoKey, SettlementAdvice.PropertyKey,
				SettlementAdvice.DeathKey,
				PackageDamageKeys.NoticeKey(PackageDamageKeys.ImpactType), PackageDamageKeys.NoticeKey(PackageDamageKeys.ElectricType),
				PackageDamageKeys.NoticeKey(string.Empty)
			};

			foreach (string key in hudKeys)
			{
				Require(key);
			}

			Assert.That(missing, Is.Empty, "Missing localization keys: " + string.Join(", ", missing));
		}

		[Test]
		public void EconomyRules_KeepTheDocumentedMvpValues()
		{
			Assert.That(EconomyRules.AmmoCostPerShot, Is.EqualTo(3));
			Assert.That(EconomyRules.PropertyDamageFine, Is.EqualTo(20));
			Assert.That(EconomyRules.DeathPenalty, Is.EqualTo(25));
		}

		private static T Load<T>(string path) where T : Object
		{
			T asset = AssetDatabase.LoadAssetAtPath<T>(path);

			Assert.That(asset, Is.Not.Null, "Missing asset: " + path);

			return asset;
		}
	}
}
#endif
