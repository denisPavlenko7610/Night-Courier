using System;
using RD.Health;
using RD.Mechanics.Combat;
using RD.Mechanics.Respawn;
using RD.Timers;
using UnityEngine;

namespace NightCourier.Player
{
	public sealed class PlayerVitals : MonoBehaviour
	{
		private const string RespawnProtectionReason = "respawn";

		[SerializeField] private HealthCarrier _health;
		[SerializeField] private Respawn _respawn;
		[SerializeField, Min(0.1f)] private float _protectionSeconds = 2f;

		private CountdownTimer _protection;
		private IDisposable _lease;

		public bool IsAlive => _health.Pool.State.IsAlive;

		public bool IsProtected => _lease != null;

		public float Fraction => (float)_health.Pool.State.Normalized;

		public int Current => Mathf.CeilToInt((float)_health.Pool.State.Current);

		public event Action Changed;

		public event Action Died;

		public event Action Respawned;

		public void SetSpawn(Pose pose)
		{
			_respawn.SetSpawnPose(pose);
		}

		private void Awake()
		{
			_respawn.SetSpawnPose(new Pose(transform.position, transform.rotation));

			_protection = new CountdownTimer(_protectionSeconds);
			_protection.Completed += EndProtection;

			_health.Pool.Changed += OnChanged;
			_health.Died += OnDied;
			_respawn.Respawned += OnRespawned;
		}

		private void Update()
		{
			if (_lease != null)
			{
				_protection.Tick(Time.deltaTime);
			}
		}

		private void OnDestroy()
		{
			if (_health != null)
			{
				_health.Pool.Changed -= OnChanged;
				_health.Died -= OnDied;
			}

			if (_respawn != null)
			{
				_respawn.Respawned -= OnRespawned;
			}

			_lease?.Dispose();
			_lease = null;
		}

		private void OnChanged(HealthResult result)
		{
			Changed?.Invoke();
		}

		private void OnDied(HealthCarrier carrier)
		{
			Died?.Invoke();
		}

		private void OnRespawned()
		{
			_lease?.Dispose();
			_lease = _health.Pool.AcquireInvulnerability(RespawnProtectionReason);
			_protection.Restart();

			Respawned?.Invoke();
		}

		private void EndProtection()
		{
			_lease?.Dispose();
			_lease = null;
			_protection.Reset();
		}
	}
}
