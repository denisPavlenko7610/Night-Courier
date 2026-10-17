using System;
using NightCourier.App;
using NightCourier.Controls;
using RD.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCourier.Player
{
	[RequireComponent(typeof(CharacterController))]
	public sealed class PlayerMotor : MonoBehaviour
	{
		private const float PitchLimitDegrees = 85f;
		private const float GroundedStickVelocity = -2f;
		private const float MouseDegreesPerCount = 0.1f;
		private const float StickDegreesPerSecond = 180f;
		private const float MinGroundNormalY = 0.5f;
		private const float KnockbackDecayPerSecond = 14f;

		[SerializeField] private Transform _cameraPoint;
		[SerializeField] private PlayerVitals _vitals;
		[SerializeField, Min(0.1f)] private float _walkSpeed = 4f;
		[SerializeField, Min(0.1f)] private float _sprintSpeed = 6.5f;
		[SerializeField, Min(0.1f)] private float _gravity = 20f;

		private CharacterController _controller;
		private PlayerInputActions _actions;
		private ISettingsService _settings;
		private float _sensitivity = GameSettings.DefaultMouseSensitivity;
		private float _pitch;
		private float _verticalVelocity;
		private Transform _ground;
		private Vector3 _groundPosition;
		private Vector3 _knockback;

		public void AddKnockback(Vector3 velocity)
		{
			_knockback = new Vector3(velocity.x, 0f, velocity.z);

			if (velocity.y > 0f)
			{
				_verticalVelocity = Mathf.Max(_verticalVelocity, velocity.y);
			}
		}

		public void Bind(PlayerInputActions actions, ISettingsService settings)
		{
			Unbind();

			_actions = actions ?? throw new ArgumentNullException(nameof(actions));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));

			_sensitivity = _settings.Get(GameSettings.MouseSensitivity);
			_settings.Changed += OnSettingChanged;
		}

		private void Awake()
		{
			_controller = GetComponent<CharacterController>();
			_vitals.Respawned += OnRespawned;
		}

		private void OnRespawned()
		{
			Physics.SyncTransforms();

			_verticalVelocity = 0f;
			_knockback = Vector3.zero;
			_pitch = 0f;
			_ground = null;
			_cameraPoint.localRotation = Quaternion.identity;
		}

		private void Update()
		{
			if (_actions == null || !_actions.Gameplay.enabled)
			{
				return;
			}

			FollowGround();
			Look();
			Move();
		}

		private void OnControllerColliderHit(ControllerColliderHit hit)
		{
			if (hit.normal.y < MinGroundNormalY)
			{
				return;
			}

			Rigidbody body = hit.collider.attachedRigidbody;
			Transform ground = body != null && body.isKinematic ? body.transform : null;

			if (ground == _ground)
			{
				return;
			}

			_ground = ground;
			_groundPosition = ground != null ? ground.position : Vector3.zero;
		}

		private void OnDestroy()
		{
			if (_vitals != null)
			{
				_vitals.Respawned -= OnRespawned;
			}

			Unbind();
		}

		private void Unbind()
		{
			if (_settings != null)
			{
				_settings.Changed -= OnSettingChanged;
			}
		}

		private void OnSettingChanged(SettingChanged change)
		{
			if (change.Id == GameSettings.MouseSensitivity.Id)
			{
				_sensitivity = _settings.Get(GameSettings.MouseSensitivity);
			}
		}

		private void FollowGround()
		{
			if (_ground == null)
			{
				return;
			}

			Vector3 position = _ground.position;
			Vector3 delta = position - _groundPosition;
			_groundPosition = position;

			if (delta != Vector3.zero)
			{
				_controller.Move(delta);
			}
		}

		private void Look()
		{
			InputAction look = _actions.Look;
			Vector2 value = look.ReadValue<Vector2>();

			if (value == Vector2.zero)
			{
				return;
			}

			bool fromPointer = look.activeControl?.device is Pointer;
			float scale = fromPointer ? MouseDegreesPerCount : StickDegreesPerSecond * Time.deltaTime;
			Vector2 degrees = value * (scale * _sensitivity);

			transform.Rotate(0f, degrees.x, 0f, Space.Self);

			_pitch = PlayerLookMath.ClampPitch(_pitch, -degrees.y, PitchLimitDegrees);
			_cameraPoint.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
		}

		private void Move()
		{
			float speed = _actions.Sprint.IsPressed() ? _sprintSpeed : _walkSpeed;
			Vector3 planar = PlayerLookMath.PlanarVelocity(_actions.Move.ReadValue<Vector2>(), transform.rotation, speed);

			if (_controller.isGrounded && _verticalVelocity < 0f)
			{
				_verticalVelocity = GroundedStickVelocity;
			}
			else
			{
				_verticalVelocity -= _gravity * Time.deltaTime;
			}

			Vector3 velocity = planar + _knockback + Vector3.up * _verticalVelocity;
			_controller.Move(velocity * Time.deltaTime);

			_knockback = Vector3.MoveTowards(_knockback, Vector3.zero, KnockbackDecayPerSecond * Time.deltaTime);
		}
	}
}
