using System;
using NightCourier.World;
using RD.Audio;
using UnityEngine;

namespace NightCourier.Audio
{
	public sealed class WarehouseAudio : MonoBehaviour
	{
		private const float LiftFadeSeconds = 0.3f;
		private const float AmbienceFadeSeconds = 0.5f;

		[SerializeField] private ConditionDoor[] _doors = Array.Empty<ConditionDoor>();
		[SerializeField] private FreightLift _lift;
		[SerializeField] private CargoScanner _scanner;
		[SerializeField] private Transform _leverPoint;
		[SerializeField] private Transform _generatorPoint;
		[SerializeField] private AudioCue _doorOpen;
		[SerializeField] private AudioCue _doorClose;
		[SerializeField] private AudioCue _lever;
		[SerializeField] private AudioCue _generator;
		[SerializeField] private AudioCue _battery;
		[SerializeField] private AudioCue _liftMotor;
		[SerializeField] private AudioCue _scannerBeep;
		[SerializeField] private AudioCue _ambience;
		[SerializeField] private AudioCue _violation;
		[SerializeField] private AudioCue _unstable;
		[SerializeField] private AudioCue _hazard;
		[SerializeField] private AudioCue _alarm;
		[SerializeField] private AudioCue _barricadeBreak;
		[SerializeField] private AudioCue _checkpoint;
		[SerializeField] private AudioCue _delivered;
		[SerializeField] private AudioCue _failed;

		private AudioService _audio;
		private WarehouseFacts _facts;
		private AudioVoiceHandle _liftVoice;
		private AudioVoiceHandle _ambienceVoice;
		private bool _powerOn;
		private bool _batteryInserted;

		public void Bind(AudioService audio, WarehouseFacts facts)
		{
			Unbind();

			if (_leverPoint == null || _generatorPoint == null)
			{
				throw new InvalidOperationException("The warehouse audio needs the lever and generator points.");
			}

			_audio = audio ?? throw new ArgumentNullException(nameof(audio));
			_facts = facts ?? throw new ArgumentNullException(nameof(facts));
			_powerOn = facts.PowerOn;
			_batteryInserted = facts.BatteryInserted;
			_ambienceVoice = _audio.Play(_ambience);

			_facts.Changed += OnFactsChanged;
			_lift.RideStarted += OnRideStarted;
			_lift.RideCompleted += OnRideCompleted;
			_scanner.ScanStarted += OnScanStarted;

			foreach (ConditionDoor door in _doors)
			{
				door.Opened += OnDoorOpened;
				door.Closed += OnDoorClosed;
			}
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_audio == null)
			{
				return;
			}

			_facts.Changed -= OnFactsChanged;
			_lift.RideStarted -= OnRideStarted;
			_lift.RideCompleted -= OnRideCompleted;
			_scanner.ScanStarted -= OnScanStarted;

			foreach (ConditionDoor door in _doors)
			{
				door.Opened -= OnDoorOpened;
				door.Closed -= OnDoorClosed;
			}

			_liftVoice.TryStop();
			_ambienceVoice.TryStop(AmbienceFadeSeconds);
			_audio = null;
			_facts = null;
		}

		public void PlayViolation()
		{
			_audio.Play(_violation);
		}

		public void PlayUnstable()
		{
			_audio.Play(_unstable);
		}

		public void PlayHazard(Vector3 position)
		{
			_audio.PlayAt(_hazard, position);
		}

		public void PlayAlarm(Vector3 position)
		{
			_audio.PlayAt(_alarm, position);
		}

		public void PlayBarricadeBreak(Vector3 position)
		{
			_audio.PlayAt(_barricadeBreak, position);
		}

		public void PlayCheckpoint()
		{
			_audio.Play(_checkpoint);
		}

		public void PlayDelivered()
		{
			_audio.Play(_delivered);
		}

		public void PlayFailed()
		{
			_audio.Play(_failed);
		}

		private void OnFactsChanged()
		{
			if (_facts.PowerOn && !_powerOn)
			{
				_audio.PlayAt(_lever, _leverPoint.position);
				_audio.PlayAt(_generator, _generatorPoint.position);
			}

			if (_facts.BatteryInserted && !_batteryInserted)
			{
				_audio.PlayAt(_battery, _generatorPoint.position);
			}

			_powerOn = _facts.PowerOn;
			_batteryInserted = _facts.BatteryInserted;
		}

		private void OnRideStarted()
		{
			_liftVoice.TryStop();
			_liftVoice = _audio.PlayFollowing(_liftMotor, _lift.PlatformTransform);
		}

		private void OnRideCompleted()
		{
			_liftVoice.TryStop(LiftFadeSeconds);
		}

		private void OnScanStarted()
		{
			_audio.PlayAt(_scannerBeep, _scanner.transform.position);
		}

		private void OnDoorOpened(ConditionDoor door)
		{
			_audio.PlayAt(_doorOpen, door.transform.position);
		}

		private void OnDoorClosed(ConditionDoor door)
		{
			_audio.PlayAt(_doorClose, door.transform.position);
		}
	}
}
