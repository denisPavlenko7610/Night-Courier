using System;
using System.Collections.Generic;
using System.Threading;
using NightCourier.Contracts;
using NightCourier.Economy;
using RD.DependencyInjection;
using RD.Foundation;
using RD.SceneFlow;
using RD.Startup;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NightCourier.App
{
	public sealed class GameHost : MonoBehaviour, IGameNavigator
	{
		[SerializeField] private SceneCatalog _sceneCatalog;
		[SerializeField] private TextAsset _localizationTable;
		[SerializeField] private ContractCatalog _contractCatalog;
		[SerializeField] private UpgradeCatalog _upgradeCatalog;
		[SerializeField] private InputActionAsset _inputActions;

		private StartupPipeline _pipeline;
		private ISceneFlow _sceneFlow;
		private Container _root;
		private Container _sceneScope;
		private GameServices _services;
		private string _currentSceneId;
		private bool _isNavigating;
		private bool _isDuplicate;

		private void Awake()
		{
			GameHost other = FindAnyObjectByType<GameHost>();

			if (other != null && other != this)
			{
				_isDuplicate = true;
				UnityLogger.LogError("A second GameHost was created. Destroying it.", this);
				Destroy(gameObject);
			}
		}

		private async void Start()
		{
			if (_isDuplicate)
			{
				return;
			}

			try
			{
				await RunAsync(destroyCancellationToken);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception, this);
			}
		}

		private async void OnDestroy()
		{
			DisposeContainers();

			if (_pipeline == null)
			{
				return;
			}

			try
			{
				await _pipeline.StopAsync();
			}
			catch (Exception exception)
			{
				UnityLogger.LogException(exception);
			}
		}

		public async Awaitable GoToAsync(string sceneId, CancellationToken cancellationToken = default)
		{
			if (_sceneFlow == null || _services == null)
			{
				throw new InvalidOperationException("The game has not finished starting.");
			}

			if (_isNavigating)
			{
				UnityLogger.LogWarning($"Ignored navigation to '{sceneId}' because another navigation is running.");
				return;
			}

			_isNavigating = true;

			try
			{
				if (_currentSceneId != null)
				{
					string previous = _currentSceneId;
					_currentSceneId = null;
					await _sceneFlow.UnloadAsync(previous, SceneIds.Boot, cancellationToken);
				}

				_sceneScope?.Dispose();
				_sceneScope = null;

				await _sceneFlow.LoadAdditiveAsync(sceneId, setActive: true, cancellationToken: cancellationToken);
				_currentSceneId = sceneId;
				_sceneScope = _root.CreateScope();

				EnterActiveScene();
			}
			finally
			{
				_isNavigating = false;
			}
		}

		public void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		private async Awaitable RunAsync(CancellationToken cancellationToken)
		{
			GameServicesBuilder builder = new();

			_pipeline = new StartupPipeline(new IStartupStep[]
			{
				new SceneFlowStep(builder, _sceneCatalog),
				new SettingsStep(builder),
				new InputStep(builder, _inputActions),
				new PauseStep(builder),
				new LocalizationStep(builder, _localizationTable),
				new CampaignStep(builder, _contractCatalog, _upgradeCatalog)
			});

			await _pipeline.StartAsync(cancellationToken);

			_sceneFlow = builder.SceneFlow;
			_root = GameComposition.Build(builder, this);
			_services = _root.Resolve<GameServices>();

			await GoToAsync(SceneIds.MainMenu, cancellationToken);
		}

		private void EnterActiveScene()
		{
			List<ISceneEntry> entries = new();

			foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
			{
				entries.AddRange(root.GetComponentsInChildren<ISceneEntry>(true));
			}

			if (entries.Count != 1)
			{
				throw new InvalidOperationException(
					$"Scene '{SceneManager.GetActiveScene().name}' must contain exactly one scene entry, found {entries.Count}.");
			}

			entries[0].Enter(_services, _sceneScope);
		}

		private void DisposeContainers()
		{
			_sceneScope?.Dispose();
			_sceneScope = null;
			_root?.Dispose();
			_root = null;
		}
	}
}
