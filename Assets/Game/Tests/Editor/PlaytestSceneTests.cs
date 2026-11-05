#if UNITY_INCLUDE_TESTS
using System.Linq;
using NightCourier.App;
using NUnit.Framework;
using RD.SceneFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NightCourier.Tests
{
	public sealed class PlaytestSceneTests
	{
		private const string PlaytestBootPath = "Assets/Game/Scenes/PlaytestBoot.unity";
		private const string NormalBootPath = "Assets/Game/Scenes/Boot.unity";
		private const string PlaytestSaveFolder = "Saves.Playtest";

		[Test]
		public void PlaytestBoot_UsesItsOwnSaveFolderAndBootCatalog()
		{
			Scene scene = EditorSceneManager.OpenPreviewScene(PlaytestBootPath);

			try
			{
				GameHost host = scene.GetRootGameObjects()
					.SelectMany(root => root.GetComponentsInChildren<GameHost>(true)).Single();
				SerializedObject serialized = new(host);
				string folder = serialized.FindProperty("_saveFolder").stringValue;
				SceneCatalog catalog = (SceneCatalog)serialized.FindProperty("_sceneCatalog").objectReferenceValue;

				Assert.That(folder, Is.EqualTo(PlaytestSaveFolder));
				Assert.That(folder, Is.Not.EqualTo(CampaignStep.SaveFolder));
				Assert.That(folder, Is.Not.EqualTo("Saves.PlayModeTests"));
				Assert.That(catalog, Is.Not.Null);
				Assert.That(catalog.GetPath(SceneIds.Boot), Is.EqualTo(PlaytestBootPath));
				Assert.That(EditorBuildSettings.scenes.First().path, Is.EqualTo(NormalBootPath));
				Assert.That(
					EditorBuildSettings.scenes.Any(item => item.enabled && item.path == PlaytestBootPath),
					Is.True
				);
			}
			finally
			{
				EditorSceneManager.ClosePreviewScene(scene);
			}
		}

		[Test]
		public void NormalBoot_StillUsesTheNormalSaveFolder()
		{
			Scene scene = EditorSceneManager.OpenPreviewScene(NormalBootPath);

			try
			{
				GameHost host = scene.GetRootGameObjects()
					.SelectMany(root => root.GetComponentsInChildren<GameHost>(true)).Single();
				SerializedObject serialized = new(host);
				SceneCatalog catalog = (SceneCatalog)serialized.FindProperty("_sceneCatalog").objectReferenceValue;

				Assert.That(serialized.FindProperty("_saveFolder").stringValue, Is.EqualTo(CampaignStep.SaveFolder));
				Assert.That(catalog.GetPath(SceneIds.Boot), Is.EqualTo(NormalBootPath));
			}
			finally
			{
				EditorSceneManager.ClosePreviewScene(scene);
			}
		}

	}
}
#endif