#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using NightCourier.Player;
using NUnit.Framework;
using RD.Mechanics.Interactable;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NightCourier.PlayModeTests
{
	internal sealed class InputBot : IDisposable
	{
		private const float MouseDegreesPerCount = 0.1f;
		private const float TurnFractionPerFrame = 0.45f;
		private const float MaxTurnDegreesPerFrame = 14f;
		private const float WalkAngleDegrees = 35f;
		private const float SprintAngleDegrees = 12f;
		private const float AimToleranceDegrees = 1.2f;
		private const float EngageToleranceDegrees = 2.5f;
		private const float StuckWindowSeconds = 1.5f;
		private const float StuckDistance = 0.25f;
		private const float RepathSeconds = 0.4f;
		private const float SnapRadius = 2.5f;
		private const float CornerArrive = 0.55f;
		private const int TapFrames = 3;

		private readonly PlayerSession _player;
		private readonly Camera _camera;
		private readonly Keyboard _keyboard;
		private readonly Mouse _mouse;
		private readonly InputSettings.BackgroundBehavior _backgroundBehavior;
		private readonly InputSettings.EditorInputBehaviorInPlayMode _editorBehavior;
		private readonly float _sensitivity;
		private readonly HashSet<Key> _held = new();
		private readonly NavMeshPath _path = new();
		private bool _leftButton;
		private Vector2 _pendingLook;

		public InputBot(PlayerSession player, float sensitivity)
		{
			_player = player;
			_camera = player.GetComponentInChildren<Camera>();
			_sensitivity = sensitivity;

			_backgroundBehavior = InputSystem.settings.backgroundBehavior;
			_editorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
			InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
			InputSystem.settings.editorInputBehaviorInPlayMode =
				InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

			_keyboard = InputSystem.AddDevice<Keyboard>("BotKeyboard");
			_mouse = InputSystem.AddDevice<Mouse>("BotMouse");
			InputSystem.EnableDevice(_keyboard);
			InputSystem.EnableDevice(_mouse);

			Assert.That(_camera, Is.Not.Null, "The player has no camera.");
		}

		public int StuckCount { get; private set; }

		public Vector3 Position => _player.transform.position;

		public void Dispose()
		{
			_held.Clear();
			_leftButton = false;
			_pendingLook = Vector2.zero;

			if (_keyboard != null && _keyboard.added)
			{
				InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
				InputSystem.RemoveDevice(_keyboard);
			}

			if (_mouse != null && _mouse.added)
			{
				InputSystem.QueueStateEvent(_mouse, new MouseState());
				InputSystem.RemoveDevice(_mouse);
			}

			InputSystem.settings.backgroundBehavior = _backgroundBehavior;
			InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehavior;
		}

		public Action FrameHook { get; set; }

		public IEnumerator Frame()
		{
			FrameHook?.Invoke();
			Flush();

			yield return null;
		}

		public IEnumerator Wait(float seconds)
		{
			float end = Time.time + seconds;

			while (Time.time < end)
			{
				yield return Frame();
			}
		}

		public IEnumerator Tap(Key key)
		{
			_held.Add(key);

			for (int frame = 0; frame < TapFrames; frame++)
			{
				yield return Frame();
			}

			_held.Remove(key);

			yield return Frame();
		}

		public void Hold(Key key)
		{
			_held.Add(key);
		}

		public void Release(Key key)
		{
			_held.Remove(key);
		}

		public void ReleaseAll()
		{
			_held.Clear();
			_leftButton = false;
		}

		public IEnumerator Engage(Func<Vector3?> point, Func<bool> done, float timeoutSeconds, float clickGapSeconds)
		{
			float end = Time.time + timeoutSeconds;
			float nextClick = 0f;
			int pressFrames = 0;

			while (Time.time < end && !done())
			{
				Vector3? target = point();

				if (!target.HasValue)
				{
					break;
				}

				Vector2 error = LookError(target);

				Turn(error);

				bool aligned = Mathf.Abs(error.x) < EngageToleranceDegrees && Mathf.Abs(error.y) < EngageToleranceDegrees;

				if (pressFrames > 0)
				{
					pressFrames--;
					_leftButton = pressFrames > 0;
				}
				else if (aligned && Time.time >= nextClick)
				{
					pressFrames = 2;
					_leftButton = true;
					nextClick = Time.time + clickGapSeconds;
				}
				else
				{
					_leftButton = false;
				}

				yield return Frame();
			}

			_leftButton = false;

			yield return Frame();
		}

		public IEnumerator Click()
		{
			_leftButton = true;

			for (int frame = 0; frame < TapFrames; frame++)
			{
				yield return Frame();
			}

			_leftButton = false;

			yield return Frame();
		}

		public void TrackAim(Vector3 world)
		{
			Turn(LookError(world));
		}

		public IEnumerator Aim(Vector3 world, float timeoutSeconds = 3f)
		{
			float end = Time.time + timeoutSeconds;
			int settled = 0;

			while (Time.time < end)
			{
				Vector2 error = LookError(world);

				if (Mathf.Abs(error.x) < AimToleranceDegrees && Mathf.Abs(error.y) < AimToleranceDegrees)
				{
					settled++;

					if (settled >= 2)
					{
						yield return Frame();

						yield break;
					}
				}
				else
				{
					settled = 0;
				}

				Turn(error);

				yield return Frame();
			}

			Assert.Fail($"Timed out aiming at {world} from {_camera.transform.position}.");
		}

		public IEnumerator Interact(Interactable target, string description, float timeoutSeconds = 3f)
		{
			Collider collider = target.GetComponentInChildren<Collider>();
			Vector3 point = collider != null ? collider.bounds.center : target.transform.position;
			float end = Time.time + timeoutSeconds;

			while (Time.time < end)
			{
				yield return Aim(point);

				if (_player.Interactor.Target == target)
				{
					yield return Tap(Key.E);

					yield break;
				}

				yield return Frame();
			}

			Assert.Fail($"The crosshair never settled on {description} (target: {_player.Interactor.Target}).");
		}

		public IEnumerator Through(Vector3 target, bool sprint, float timeoutSeconds = 12f, float arrive = 0.4f)
		{
			float end = Time.time + timeoutSeconds;
			Vector3 mark = Position;
			float markTime = Time.time;

			while (Time.time < end)
			{
				Vector3 offset = target - Position;
				offset.y = 0f;

				if (offset.magnitude < arrive)
				{
					ReleaseWalk();

					yield break;
				}

				Steer(offset, sprint);

				if (Time.time - markTime > StuckWindowSeconds)
				{
					if ((Position - mark).magnitude < StuckDistance)
					{
						StuckCount++;
						ReleaseWalk();

						Assert.Fail($"The bot is stuck at {Position} heading for {target}.");
					}

					mark = Position;
					markTime = Time.time;
				}

				yield return Frame();
			}

			ReleaseWalk();
			Assert.Fail($"Timed out walking to {target} from {Position}.");
		}

		public IEnumerator GoTo(Vector3 target, bool sprint, float timeoutSeconds = 30f, float arrive = 0.7f)
		{
			Assert.That(
				NavMesh.SamplePosition(target, out NavMeshHit end, SnapRadius, NavMesh.AllAreas),
				Is.True,
				$"No NavMesh near {target}.");

			float deadline = Time.time + timeoutSeconds;
			float nextRepath = 0f;
			Vector3[] corners = Array.Empty<Vector3>();
			int index = 1;
			Vector3 mark = Position;
			float markTime = Time.time;

			while (Time.time < deadline)
			{
				Vector3 flat = end.position - Position;
				flat.y = 0f;

				if (flat.magnitude < arrive)
				{
					ReleaseWalk();

					yield break;
				}

				if (Time.time >= nextRepath)
				{
					nextRepath = Time.time + RepathSeconds;

					Assert.That(
						NavMesh.SamplePosition(Position, out NavMeshHit start, SnapRadius, NavMesh.AllAreas),
						Is.True,
						$"The player at {Position} is off the NavMesh.");

					NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, _path);

					Assert.That(
						_path.status,
						Is.EqualTo(NavMeshPathStatus.PathComplete),
						$"No complete path from {Position} to {target}.");

					corners = _path.corners;
					index = Mathf.Min(1, corners.Length - 1);
				}

				while (index < corners.Length - 1 && Flat(corners[index] - Position).magnitude < CornerArrive)
				{
					index++;
				}

				Steer(Flat(corners[Mathf.Min(index, corners.Length - 1)] - Position), sprint);

				if (Time.time - markTime > StuckWindowSeconds)
				{
					if ((Position - mark).magnitude < StuckDistance)
					{
						StuckCount++;
						ReleaseWalk();

						Assert.Fail($"The bot is stuck at {Position} heading for {target}.");
					}

					mark = Position;
					markTime = Time.time;
				}

				yield return Frame();
			}

			ReleaseWalk();
			Assert.Fail($"Timed out walking to {target} from {Position}.");
		}

		private static Vector3 Flat(Vector3 value)
		{
			value.y = 0f;

			return value;
		}

		private void Steer(Vector3 offset, bool sprint)
		{
			float wanted = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
			float yawError = Mathf.DeltaAngle(_player.transform.eulerAngles.y, wanted);

			Turn(new Vector2(yawError, LookError(null).y));

			bool aligned = Mathf.Abs(yawError) < WalkAngleDegrees;

			SetKey(Key.W, aligned);
			SetKey(Key.LeftShift, sprint && Mathf.Abs(yawError) < SprintAngleDegrees);
		}

		private void ReleaseWalk()
		{
			_held.Remove(Key.W);
			_held.Remove(Key.LeftShift);
		}

		private void SetKey(Key key, bool pressed)
		{
			if (pressed)
			{
				_held.Add(key);
			}
			else
			{
				_held.Remove(key);
			}
		}

		private Vector2 LookError(Vector3? world)
		{
			float pitch = Mathf.DeltaAngle(0f, _camera.transform.eulerAngles.x);

			if (!world.HasValue)
			{
				return new Vector2(0f, -pitch);
			}

			Vector3 to = world.Value - _camera.transform.position;
			float yaw = Mathf.DeltaAngle(_player.transform.eulerAngles.y, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
			float wantedPitch = -Mathf.Asin(Mathf.Clamp(to.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;

			return new Vector2(yaw, wantedPitch - pitch);
		}

		private void Turn(Vector2 errorDegrees)
		{
			float yaw = Mathf.Clamp(errorDegrees.x * TurnFractionPerFrame, -MaxTurnDegreesPerFrame, MaxTurnDegreesPerFrame);
			float pitch = Mathf.Clamp(errorDegrees.y * TurnFractionPerFrame, -MaxTurnDegreesPerFrame, MaxTurnDegreesPerFrame);
			float countsPerDegree = 1f / (MouseDegreesPerCount * _sensitivity);

			_pendingLook = new Vector2(yaw * countsPerDegree, -pitch * countsPerDegree);
		}

		private void Flush()
		{
			KeyboardState keys = new();

			foreach (Key key in _held)
			{
				keys.Set(key, true);
			}

			InputSystem.QueueStateEvent(_keyboard, keys);

			MouseState mouse = new()
			{
				delta = _pendingLook,
				buttons = (ushort)(_leftButton ? 1 : 0)
			};

			InputSystem.QueueStateEvent(_mouse, mouse);

			_pendingLook = Vector2.zero;
		}
	}
}
#endif
