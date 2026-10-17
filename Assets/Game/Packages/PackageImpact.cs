using System;
using RD.Health;
using RD.Mechanics.Combat;
using RD.Mechanics.PickupDrop;
using UnityEngine;

namespace NightCourier.Packages
{
	public readonly struct PackageImpactInfo
	{
		public PackageImpactInfo(float speed, bool onSafeSurface, bool afterRelease, Collider surface = null)
		{
			Speed = speed;
			OnSafeSurface = onSafeSurface;
			AfterRelease = afterRelease;
			Surface = surface;
		}

		public Collider Surface { get; }

		public float Speed { get; }

		public bool OnSafeSurface { get; }

		public bool AfterRelease { get; }
	}

	public sealed class PackageImpact : MonoBehaviour
	{
		private const string DamageSourceId = "package.impact";

		private static readonly DamageTypeId ImpactDamageType = new("impact");

		[SerializeField] private PickupItem _item;
		[SerializeField] private HealthCarrier _health;
		[SerializeField, Min(0f)] private float _damageSpeed = 4f;
		[SerializeField, Min(0.1f)] private float _damagePerSpeed = 10f;

		private bool _afterRelease;
		private bool _hasPending;
		private float _pendingSpeed;
		private bool _pendingSafe;
		private bool _pendingAfterRelease;
		private Collider _pendingSurface;

		public float DamageMultiplier { get; set; } = 1f;

		public event Action<PackageImpactInfo> Impacted;

		public static double CalculateDamage(float speed, float damageSpeed, float damagePerSpeed)
		{
			return speed <= damageSpeed ? 0d : (speed - damageSpeed) * damagePerSpeed;
		}

		public void Arm()
		{
			enabled = true;
		}

		private void OnEnable()
		{
			_item.Released += OnReleased;
		}

		private void OnDisable()
		{
			_item.Released -= OnReleased;
		}

		private void OnReleased(PickupItem item)
		{
			_afterRelease = true;
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (collision.collider is CharacterController)
			{
				return;
			}

			bool onSafeSurface = collision.collider.GetComponentInParent<PackageSafeSurface>() != null;

			if (!_hasPending)
			{
				_hasPending = true;
				_pendingAfterRelease = _afterRelease;
				_pendingSurface = collision.collider;
				_afterRelease = false;
			}

			if (onSafeSurface && !_pendingSafe)
			{
				_pendingSurface = collision.collider;
			}

			_pendingSafe |= onSafeSurface;
			_pendingSpeed = Mathf.Max(_pendingSpeed, collision.relativeVelocity.magnitude);
		}

		private void FixedUpdate()
		{
			if (!_hasPending)
			{
				return;
			}

			PackageImpactInfo info = new(_pendingSpeed, _pendingSafe, _pendingAfterRelease, _pendingSurface);

			_hasPending = false;
			_pendingSpeed = 0f;
			_pendingSafe = false;
			_pendingAfterRelease = false;
			_pendingSurface = null;

			if (!info.OnSafeSurface)
			{
				double damage = CalculateDamage(info.Speed, _damageSpeed, _damagePerSpeed) * DamageMultiplier;

				if (damage > 0d)
				{
					_health.ReceiveDamage(new DamageRequest(damage, ImpactDamageType, DamageSourceId));
				}
			}

			Impacted?.Invoke(info);
		}
	}
}
