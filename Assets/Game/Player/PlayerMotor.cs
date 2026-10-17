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

		[SerializeField] private Transform _cameraPoint;
		[SerializeField, Min(0.1f)] private float _walkSpeed = 4f;
		[SerializeField, Min(0.1f)] private float _sprintSpeed = 6.5f;
		[SerializeField, Min(0.1f)] private float _gravity = 20f;

		private CharacterController _controller;
		private PlayerInputActions _actions;
		private ISettingsService _settings;
		private float _sensitivity = GameSettings.DefaultMouseSensitivity;
		private float _pitch;
		private float _verticalVelocity;

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
		}

		private void Update()
		{
			if (_actions == null || !_actions.Gameplay.enabled)
			{
				return;
			}

			Look();
			Move();
		}

		private void OnDestroy()
		{
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

			Vector3 velocity = planar + Vector3.up * _verticalVelocity;
			_controller.Move(velocity * Time.deltaTime);
		}
	}
}
