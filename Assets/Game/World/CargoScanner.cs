using System;
using NightCourier.Player;
using RD.Mechanics.PressurePlate;
using RD.Timers;
using UnityEngine;

namespace NightCourier.World
{
	public sealed class CargoScanner : MonoBehaviour
	{
		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		[SerializeField] private PressurePlate _plate;
		[SerializeField] private Renderer _indicator;
		[SerializeField, Min(0.5f)] private float _scanSeconds = 3f;
		[SerializeField] private Color _idleColor = new(0.25f, 0.25f, 0.25f);
		[SerializeField] private Color _scanningColor = new(1f, 0.65f, 0.1f);
		[SerializeField] private Color _scannedColor = new(0.2f, 0.9f, 0.35f);

		private MaterialPropertyBlock _block;
		private CountdownTimer _timer;
		private WarehouseFacts _facts;
		private CargoPackage _package;
		private Collider _plateCollider;
		private Collider[] _packageColliders = Array.Empty<Collider>();

		public event Action ScanStarted;

		public event Action ScanInterrupted;

		public bool IsScanning => _timer != null && _timer.IsRunning;

		public float Progress => _timer == null ? 0f : (float)_timer.Progress;

		public bool HasPackageOnPlate
		{
			get
			{
				if (_package == null || _plateCollider == null || !_plate.IsActive || _package.IsHeld)
				{
					return false;
				}

				Bounds plate = _plateCollider.bounds;

				foreach (Collider collider in _packageColliders)
				{
					if (collider != null && plate.Intersects(collider.bounds))
					{
						return true;
					}
				}

				return false;
			}
		}

		public void Bind(WarehouseFacts facts, CargoPackage package)
		{
			Unbind();

			_facts = facts ?? throw new ArgumentNullException(nameof(facts));
			_package = package != null ? package : throw new ArgumentNullException(nameof(package));
			_plateCollider = _plate.GetComponent<Collider>();
			_packageColliders = Array.FindAll(package.GetComponentsInChildren<Collider>(), collider => !collider.isTrigger);
			_block ??= new MaterialPropertyBlock();
			_timer = new CountdownTimer(_scanSeconds);
			_timer.Completed += OnScanCompleted;

			_facts.Changed += OnFactsChanged;

			OnFactsChanged();
		}

		private void Update()
		{
			if (_timer == null)
			{
				return;
			}

			Evaluate();

			if (!IsScanning)
			{
				return;
			}

			_timer.Tick(Time.deltaTime);
			Paint();
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_facts != null)
			{
				_facts.Changed -= OnFactsChanged;
				_facts = null;
			}

			if (_timer != null)
			{
				_timer.Completed -= OnScanCompleted;
				_timer = null;
			}

			_package = null;
		}

		private void OnFactsChanged()
		{
			Evaluate();
			Paint();
		}

		private void Evaluate()
		{
			bool shouldScan = !_facts.CargoScanned && _facts.PowerOn && HasPackageOnPlate;

			if (shouldScan && _timer.State == CountdownTimerState.Idle)
			{
				_timer.Restart();
				ScanStarted?.Invoke();
				Paint();
			}
			else if (!shouldScan && _timer.State == CountdownTimerState.Running)
			{
				bool finished = _facts.CargoScanned;

				_timer.Reset();
				Paint();

				if (!finished)
				{
					ScanInterrupted?.Invoke();
				}
			}
		}

		private void OnScanCompleted()
		{
			_facts.SetCargoScanned(true);
		}

		private void Paint()
		{
			Color color = _facts.CargoScanned
				? _scannedColor
				: IsScanning
					? Color.Lerp(_idleColor, _scanningColor, 0.4f + 0.6f * Progress)
					: _idleColor;

			_block.SetColor(BaseColorId, color);
			_indicator.SetPropertyBlock(_block);
		}
	}
}
