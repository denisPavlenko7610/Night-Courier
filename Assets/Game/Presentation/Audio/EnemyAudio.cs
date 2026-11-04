using System;
using NightCourier.AI;
using RD.Audio;
using RD.Health;
using RD.Mechanics.Combat;
using RD.Mechanics.MeleeAttack;
using UnityEngine;

namespace NightCourier.Audio
{
	public sealed class EnemyAudio : MonoBehaviour
	{
		private const float HumFadeSeconds = 0.2f;

		[SerializeField] private HealthCarrier _health;
		[SerializeField] private EnemyDeath _death;
		[SerializeField] private MeleeAttack _melee;
		[SerializeField] private SecurityDroneBrain _drone;
		[SerializeField] private AudioCue _hit;
		[SerializeField] private AudioCue _die;
		[SerializeField] private AudioCue _attack;
		[SerializeField] private AudioCue _hum;
		[SerializeField] private AudioCue _bolt;

		private AudioService _audio;
		private AudioVoiceHandle _humVoice;

		public void Bind(AudioService audio)
		{
			Unbind();

			_audio = audio ?? throw new ArgumentNullException(nameof(audio));

			_health.DamageResolved += OnDamageResolved;
			_death.Died += OnDied;

			if (_melee != null)
			{
				_melee.AttackStarted += OnAttackStarted;
			}

			if (_drone != null)
			{
				_drone.BoltFired += OnBoltFired;
				_humVoice = _audio.PlayFollowing(_hum, transform);
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

			_health.DamageResolved -= OnDamageResolved;
			_death.Died -= OnDied;

			if (_melee != null)
			{
				_melee.AttackStarted -= OnAttackStarted;
			}

			if (_drone != null)
			{
				_drone.BoltFired -= OnBoltFired;
			}

			_humVoice.TryStop(HumFadeSeconds);
			_audio = null;
		}

		private void OnDamageResolved(DamageRequest request, HealthResult result)
		{
			if (result.Delta < 0d && result.Current.IsAlive)
			{
				_audio.PlayAt(_hit, transform.position);
			}
		}

		private void OnDied()
		{
			_humVoice.TryStop(HumFadeSeconds);
			_audio.PlayAt(_die, transform.position);
		}

		private void OnAttackStarted()
		{
			_audio.PlayAt(_attack, transform.position);
		}

		private void OnBoltFired()
		{
			_audio.PlayAt(_bolt, transform.position);
		}
	}
}
