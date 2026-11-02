using System;
using NightCourier.Player;
using NightCourier.Weapons;
using RD.Audio;
using RD.Mechanics.AmmoReload;
using UnityEngine;

namespace NightCourier.Audio
{
	public sealed class PlayerAudio : MonoBehaviour
	{
		private const float StrideMeters = 2.2f;
		private const float MinimumWalkSpeed = 0.6f;

		[SerializeField] private StunPistol _pistol;
		[SerializeField] private Crowbar _crowbar;
		[SerializeField] private PlayerVitals _vitals;
		[SerializeField] private PlayerMedkits _medkits;
		[SerializeField] private PlayerCarry _carry;
		[SerializeField] private PlayerHeavyCarry _heavy;
		[SerializeField] private CharacterController _controller;
		[SerializeField] private AudioCue _pistolShot;
		[SerializeField] private AudioCue _pistolDry;
		[SerializeField] private AudioCue _pistolReload;
		[SerializeField] private AudioCue _pistolReady;
		[SerializeField] private AudioCue _crowbarSwing;
		[SerializeField] private AudioCue _crowbarHit;
		[SerializeField] private AudioCue _footstep;
		[SerializeField] private AudioCue _hurt;
		[SerializeField] private AudioCue _death;
		[SerializeField] private AudioCue _respawn;
		[SerializeField] private AudioCue _medkit;
		[SerializeField] private AudioCue _pickUp;
		[SerializeField] private AudioCue _drop;

		private AudioService _audio;
		private int _health;
		private float _stride;
		private bool _carrying;
		private bool _heavyCarrying;

		public void Bind(AudioService audio)
		{
			Unbind();

			_audio = audio ?? throw new ArgumentNullException(nameof(audio));
			_health = _vitals.Current;
			_carrying = _carry.IsCarrying;
			_heavyCarrying = _heavy.IsCarrying;
			_stride = 0f;

			_pistol.Fired += OnPistolFired;
			_pistol.DryFired += OnPistolDry;
			_pistol.Ammo.ReloadStarted += OnReloadStarted;
			_pistol.Ammo.ReloadCompleted += OnReloadCompleted;
			_crowbar.Swung += OnSwung;
			_crowbar.Hit += OnCrowbarHit;
			_vitals.Changed += OnVitalsChanged;
			_vitals.Died += OnDied;
			_vitals.Respawned += OnRespawned;
			_medkits.Used += OnMedkitUsed;
			_carry.HeldChanged += OnCarryChanged;
			_heavy.HeldChanged += OnHeavyChanged;
		}

		private void Update()
		{
			if (_audio == null || Time.deltaTime <= 0f || !_vitals.IsAlive)
			{
				return;
			}

			Vector3 velocity = _controller.velocity;
			velocity.y = 0f;

			float speed = velocity.magnitude;

			if (!_controller.isGrounded || speed < MinimumWalkSpeed)
			{
				return;
			}

			_stride += speed * Time.deltaTime;

			if (_stride >= StrideMeters)
			{
				_stride = 0f;
				_audio.Play(_footstep);
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

			_pistol.Fired -= OnPistolFired;
			_pistol.DryFired -= OnPistolDry;
			_pistol.Ammo.ReloadStarted -= OnReloadStarted;
			_pistol.Ammo.ReloadCompleted -= OnReloadCompleted;
			_crowbar.Swung -= OnSwung;
			_crowbar.Hit -= OnCrowbarHit;
			_vitals.Changed -= OnVitalsChanged;
			_vitals.Died -= OnDied;
			_vitals.Respawned -= OnRespawned;
			_medkits.Used -= OnMedkitUsed;
			_carry.HeldChanged -= OnCarryChanged;
			_heavy.HeldChanged -= OnHeavyChanged;
			_audio = null;
		}

		private void OnPistolFired(RD.Mechanics.Combat.CombatHit hit)
		{
			_audio.Play(_pistolShot);
		}

		private void OnPistolDry()
		{
			_audio.Play(_pistolDry);
		}

		private void OnReloadStarted(AmmoReload ammo)
		{
			_audio.Play(_pistolReload);
		}

		private void OnReloadCompleted(AmmoReload ammo)
		{
			_audio.Play(_pistolReady);
		}

		private void OnSwung()
		{
			_audio.Play(_crowbarSwing);
		}

		private void OnCrowbarHit(RD.Mechanics.Combat.HealthCarrier target)
		{
			_audio.Play(_crowbarHit);
		}

		private void OnVitalsChanged()
		{
			int current = _vitals.Current;

			if (current < _health && _vitals.IsAlive)
			{
				_audio.Play(_hurt);
			}

			_health = current;
		}

		private void OnDied()
		{
			_audio.Play(_death);
		}

		private void OnRespawned()
		{
			_health = _vitals.Current;
			_audio.Play(_respawn);
		}

		private void OnMedkitUsed()
		{
			_audio.Play(_medkit);
		}

		private void OnCarryChanged()
		{
			bool now = _carry.IsCarrying;

			if (now != _carrying)
			{
				_audio.Play(now ? _pickUp : _drop);
			}

			_carrying = now;
		}

		private void OnHeavyChanged()
		{
			bool now = _heavy.IsCarrying;

			if (now != _heavyCarrying)
			{
				_audio.Play(now ? _pickUp : _drop);
			}

			_heavyCarrying = now;
		}
	}
}
