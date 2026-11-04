#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using NightCourier.Audio;
using NightCourier.World;
using NUnit.Framework;
using RD.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NightCourier.Tests
{
	public sealed class AudioTests
	{
		private static readonly Vector3 LeverPosition = new(-19.7f, 0f, -7f);
		private static readonly Vector3 GeneratorPosition = new(-19.5f, 0f, -10.5f);
		private static readonly Vector3 PackagePosition = new(-15.6f, 1f, -11.9f);

		private readonly List<Object> _created = new();

		private GameObject _owner;
		private AudioService _audio;
		private WarehouseAudio _warehouse;
		private WarehouseFacts _facts;
		private AudioClip _leverClip;
		private AudioClip _generatorClip;
		private AudioClip _batteryClip;
		private AudioClip _hazardClip;

		[SetUp]
		public void SetUp()
		{
			_owner = Track(new GameObject("PH_AudioOwner"));
			_audio = new AudioService(_owner.transform, new AudioServiceOptions(maxVoiceCount: 8, prewarmVoiceCount: 2));

			GameObject host = Track(new GameObject("PH_WarehouseAudio"));
			Transform lever = Track(new GameObject("PH_LeverPoint")).transform;
			Transform generator = Track(new GameObject("PH_GeneratorPoint")).transform;

			lever.position = LeverPosition;
			generator.position = GeneratorPosition;

			_warehouse = host.AddComponent<WarehouseAudio>();
			_facts = new WarehouseFacts();

			SerializedObject serialized = new(_warehouse);

			Set(serialized, "_lift", host.AddComponent<FreightLift>());
			Set(serialized, "_scanner", host.AddComponent<CargoScanner>());
			Set(serialized, "_leverPoint", lever);
			Set(serialized, "_generatorPoint", generator);
			Set(serialized, "_lever", Cue("lever", out _leverClip));
			Set(serialized, "_generator", Cue("generator", out _generatorClip));
			Set(serialized, "_battery", Cue("battery", out _batteryClip));
			Set(serialized, "_hazard", Cue("hazard", out _hazardClip));
			Set(serialized, "_ambience", Cue("ambience", out _));

			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		[TearDown]
		public void TearDown()
		{
			_audio.Dispose();

			foreach (Object created in _created)
			{
				if (created != null)
				{
					Object.DestroyImmediate(created);
				}
			}

			_created.Clear();
		}

		[Test]
		public void PowerOn_PlaysTheLeverAtTheLeverAndTheGeneratorAtTheGenerator()
		{
			_warehouse.Bind(_audio, _facts);

			_facts.SetPower(true);

			Assert.That(PositionOf(_leverClip), Is.EqualTo(LeverPosition).Using(Vector3Comparer.Instance));
			Assert.That(PositionOf(_generatorClip), Is.EqualTo(GeneratorPosition).Using(Vector3Comparer.Instance));
		}

		[Test]
		public void BatteryInserted_PlaysAtTheGenerator()
		{
			_warehouse.Bind(_audio, _facts);
			_facts.SetBatteryRequired(true);

			_facts.SetBatteryInserted(true);

			Assert.That(PositionOf(_batteryClip), Is.EqualTo(GeneratorPosition).Using(Vector3Comparer.Instance));
		}

		[Test]
		public void Hazard_PlaysAtTheGivenPosition()
		{
			_warehouse.Bind(_audio, _facts);

			_warehouse.PlayHazard(PackagePosition);

			Assert.That(PositionOf(_hazardClip), Is.EqualTo(PackagePosition).Using(Vector3Comparer.Instance));
		}

		[Test]
		public void Bind_WithoutTheLeverAndGeneratorPoints_FailsFast()
		{
			SerializedObject serialized = new(_warehouse);

			Set(serialized, "_leverPoint", null);
			serialized.ApplyModifiedPropertiesWithoutUndo();

			Assert.Throws<InvalidOperationException>(() => _warehouse.Bind(_audio, _facts));
		}

		[Test]
		public void WarehouseScene_HasEveryAudioReferenceAssigned()
		{
			Scene("Assets/Game/Scenes/Warehouse.unity", scene =>
			{
				WarehouseAudio audio = FindIn<WarehouseAudio>(scene);
				AssertNoMissingReferences(audio, "WarehouseScene");
				Assert.That(new SerializedObject(audio).FindProperty("_doors").arraySize, Is.EqualTo(2));

				Transform lever = FindByName(scene, "PowerLever");
				Transform generator = FindByName(scene, "Generator");
				SerializedObject serialized = new(audio);

				Assert.That(serialized.FindProperty("_leverPoint").objectReferenceValue, Is.EqualTo(lever));
				Assert.That(serialized.FindProperty("_generatorPoint").objectReferenceValue, Is.EqualTo(generator));
				AssertAllWired<UiSounds>(scene, 2);
			});
		}

		[Test]
		public void DepotScene_HasEveryUiSoundAssigned()
		{
			Scene("Assets/Game/Scenes/Depot.unity", scene => AssertAllWired<UiSounds>(scene, 4));
		}

		[Test]
		public void MainMenuScene_HasEveryUiSoundAssigned()
		{
			Scene("Assets/Game/Scenes/MainMenu.unity", scene => AssertAllWired<UiSounds>(scene, 1));
		}

		[Test]
		public void PlayerPrefab_HasEveryAudioReferenceAssigned()
		{
			Prefab("Assets/Game/Content/Prefabs/Player/Player.prefab", root =>
				AssertNoMissingReferences(root.GetComponentInChildren<PlayerAudio>(true), "Player"));
		}

		[Test]
		public void CrawlerPrefab_HasEveryAudioReferenceAssigned()
		{
			Prefab("Assets/Game/Content/Prefabs/Enemies/Crawler.prefab", root =>
				AssertNoMissingReferences(root.GetComponentInChildren<EnemyAudio>(true), "Crawler", "_drone", "_hum", "_bolt"));
		}

		[Test]
		public void DronePrefab_HasEveryAudioReferenceAssigned()
		{
			Prefab("Assets/Game/Content/Prefabs/Enemies/SecurityDrone.prefab", root =>
				AssertNoMissingReferences(root.GetComponentInChildren<EnemyAudio>(true), "Drone", "_melee", "_attack"));
		}

		[TestCase("Assets/Game/Content/UI/PauseMenu.prefab")]
		[TestCase("Assets/Game/Content/UI/ConfirmWindow.prefab")]
		public void MenuPrefabs_HaveEveryUiSoundAssigned(string path)
		{
			Prefab(path, root =>
			{
				UiSounds[] sounds = root.GetComponentsInChildren<UiSounds>(true);

				Assert.That(sounds, Is.Not.Empty, path);

				foreach (UiSounds item in sounds)
				{
					AssertNoMissingReferences(item, path);
				}
			});
		}

		private static void Scene(string path, Action<UnityEngine.SceneManagement.Scene> check)
		{
			UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

			try
			{
				check(scene);
			}
			finally
			{
				EditorSceneManager.CloseScene(scene, true);
			}
		}

		private static void Prefab(string path, Action<GameObject> check)
		{
			GameObject root = PrefabUtility.LoadPrefabContents(path);

			try
			{
				check(root);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private static T FindIn<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
		{
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				T found = root.GetComponentInChildren<T>(true);

				if (found != null)
				{
					return found;
				}
			}

			Assert.Fail($"{typeof(T).Name} was not found in scene {scene.name}.");

			return null;
		}

		private static Transform FindByName(UnityEngine.SceneManagement.Scene scene, string name)
		{
			foreach (GameObject root in scene.GetRootGameObjects())
			{
				foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
				{
					if (child.name == name)
					{
						return child;
					}
				}
			}

			Assert.Fail($"Object '{name}' was not found in scene {scene.name}.");

			return null;
		}

		private static void AssertAllWired<T>(UnityEngine.SceneManagement.Scene scene, int expectedCount) where T : Component
		{
			int count = 0;

			foreach (GameObject root in scene.GetRootGameObjects())
			{
				foreach (T component in root.GetComponentsInChildren<T>(true))
				{
					AssertNoMissingReferences(component, scene.name);
					count++;
				}
			}

			Assert.That(count, Is.EqualTo(expectedCount), $"{typeof(T).Name} count in {scene.name}");
		}

		private static void AssertNoMissingReferences(Object target, string label, params string[] optional)
		{
			Assert.That(target, Is.Not.Null, label);

			SerializedProperty property = new SerializedObject(target).GetIterator();
			List<string> missing = new();

			while (property.NextVisible(true))
			{
				if (property.propertyType != SerializedPropertyType.ObjectReference
					|| property.propertyPath == "m_Script"
					|| property.objectReferenceValue != null
					|| Array.IndexOf(optional, property.propertyPath) >= 0)
				{
					continue;
				}

				missing.Add(property.propertyPath);
			}

			Assert.That(missing, Is.Empty, $"{label}: unassigned references {string.Join(", ", missing)}");
		}

		private AudioCue Cue(string name, out AudioClip clip)
		{
			clip = Track(AudioClip.Create("PH_" + name, 4410, 1, 44100, false));

			AudioCue cue = Track(ScriptableObject.CreateInstance<AudioCue>());
			SerializedObject serialized = new(cue);
			SerializedProperty clips = serialized.FindProperty("_clips");

			clips.arraySize = 1;
			clips.GetArrayElementAtIndex(0).FindPropertyRelative("_clip").objectReferenceValue = clip;
			clips.GetArrayElementAtIndex(0).FindPropertyRelative("_weight").floatValue = 1f;
			serialized.FindProperty("_spatialBlend").floatValue = 1f;
			serialized.ApplyModifiedPropertiesWithoutUndo();

			return cue;
		}

		private Vector3 PositionOf(AudioClip clip)
		{
			foreach (AudioSource source in _owner.GetComponentsInChildren<AudioSource>(true))
			{
				if (source.clip == clip)
				{
					return source.transform.position;
				}
			}

			Assert.Fail($"No voice used clip '{clip.name}'.");

			return default;
		}

		private T Track<T>(T created) where T : Object
		{
			_created.Add(created);

			return created;
		}

		private static void Set(SerializedObject serialized, string field, Object value)
		{
			SerializedProperty property = serialized.FindProperty(field);

			Assert.That(property, Is.Not.Null, field);

			property.objectReferenceValue = value;
		}

		private sealed class Vector3Comparer : IEqualityComparer<Vector3>
		{
			public static readonly Vector3Comparer Instance = new();

			public bool Equals(Vector3 x, Vector3 y)
			{
				return (x - y).sqrMagnitude < 1e-6f;
			}

			public int GetHashCode(Vector3 value)
			{
				return value.GetHashCode();
			}
		}
	}
}
#endif
