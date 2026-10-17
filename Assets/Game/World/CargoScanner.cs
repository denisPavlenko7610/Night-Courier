using System;
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

		public bool IsScanning => _timer != null && _timer.IsRunning;

		public float Progress => _timer == null ? 0f : (float)_timer.Progress;

		public void Bind(WarehouseFacts facts)
		{
			Unbind();

			_facts = facts ?? throw new ArgumentNullException(nameof(facts));
			_block ??= new MaterialPropertyBlock();
			_timer = new CountdownTimer(_scanSeconds);
			_timer.Completed += OnScanCompleted;

			_plate.Activated += Evaluate;
			_plate.Deactivated += Evaluate;
			_facts.Changed += Evaluate;

			Evaluate();
		}

		private void Update()
		{
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
			if (_plate != null)
			{
				_plate.Activated -= Evaluate;
				_plate.Deactivated -= Evaluate;
			}

			if (_facts != null)
			{
				_facts.Changed -= Evaluate;
				_facts = null;
			}

			if (_timer != null)
			{
				_timer.Completed -= OnScanCompleted;
				_timer = null;
			}
		}

		private void Evaluate()
		{
			bool shouldScan = !_facts.CargoScanned && _facts.PowerOn && _plate.IsActive;

			if (shouldScan && _timer.State == CountdownTimerState.Idle)
			{
				_timer.Restart();
			}
			else if (!shouldScan && _timer.State == CountdownTimerState.Running)
			{
				_timer.Reset();
			}

			Paint();
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
