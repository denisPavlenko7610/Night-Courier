using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace NightCourier.Controls
{
	public enum ControlScheme
	{
		KeyboardMouse,
		Gamepad
	}

	public enum GamepadStyle
	{
		Xbox,
		PlayStation
	}

	public static class ControlLabels
	{
		public const string ListSeparator = " / ";

		private const string KeyboardPrefix = "<Keyboard>/";
		private const string MousePrefix = "<Mouse>/";
		private const string GamepadPrefix = "<Gamepad>/";
		private const string PartSeparator = " ";
		private const string UpPart = "Up";
		private const string LeftPart = "Left";
		private const string DownPart = "Down";
		private const string RightPart = "Right";

		private static readonly string[] CompositeOrder = { UpPart, LeftPart, DownPart, RightPart };

		public static bool Matches(string path, ControlScheme scheme)
		{
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}

			return scheme == ControlScheme.Gamepad
				? path.StartsWith(GamepadPrefix, StringComparison.Ordinal)
				: path.StartsWith(KeyboardPrefix, StringComparison.Ordinal)
					|| path.StartsWith(MousePrefix, StringComparison.Ordinal);
		}

		public static string FindPath(InputAction action, ControlScheme scheme)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			foreach (InputBinding binding in action.bindings)
			{
				if (!binding.isComposite && !binding.isPartOfComposite && Matches(binding.effectivePath, scheme))
				{
					return binding.effectivePath;
				}
			}

			return null;
		}

		public static string PromptLabel(InputAction action, ControlScheme scheme, GamepadStyle style)
		{
			string path = FindPath(action, scheme);

			return path == null ? string.Empty : PromptLabel(path, style);
		}

		public static string PromptLabel(string path, GamepadStyle style)
		{
			if (path != null && path.StartsWith(GamepadPrefix, StringComparison.Ordinal))
			{
				string face = FaceButton(path.Substring(GamepadPrefix.Length), style);

				if (face != null)
				{
					return face;
				}
			}

			return Label(path);
		}

		public static string Label(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			if (path.StartsWith(GamepadPrefix, StringComparison.Ordinal))
			{
				return GamepadLabel(path.Substring(GamepadPrefix.Length), path);
			}

			if (path.StartsWith(MousePrefix, StringComparison.Ordinal))
			{
				return MouseLabel(path.Substring(MousePrefix.Length), path);
			}

			if (path.StartsWith(KeyboardPrefix, StringComparison.Ordinal)
				&& path.Substring(KeyboardPrefix.Length) == "escape")
			{
				return "Esc";
			}

			return Readable(path);
		}

		public static string Describe(InputAction action, ControlScheme scheme)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			List<string> entries = new();
			ReadOnlyArray<InputBinding> bindings = action.bindings;

			for (int index = 0; index < bindings.Count; index++)
			{
				InputBinding binding = bindings[index];

				if (binding.isComposite)
				{
					string composite = DescribeComposite(bindings, index, scheme);

					if (composite.Length > 0)
					{
						entries.Add(composite);
					}

					continue;
				}

				if (!binding.isPartOfComposite && Matches(binding.effectivePath, scheme))
				{
					entries.Add(Label(binding.effectivePath));
				}
			}

			return string.Join(ListSeparator, entries);
		}

		public static string Describe(ControlScheme scheme, params InputAction[] actions)
		{
			List<string> entries = new(actions.Length);

			foreach (InputAction action in actions)
			{
				string text = Describe(action, scheme);

				if (text.Length > 0)
				{
					entries.Add(text);
				}
			}

			return string.Join(ListSeparator, entries);
		}

		private static string DescribeComposite(ReadOnlyArray<InputBinding> bindings, int compositeIndex, ControlScheme scheme)
		{
			Dictionary<string, string> parts = new();
			List<string> ordered = new();

			for (int index = compositeIndex + 1; index < bindings.Count && bindings[index].isPartOfComposite; index++)
			{
				InputBinding part = bindings[index];

				if (!Matches(part.effectivePath, scheme))
				{
					continue;
				}

				parts[part.name] = Label(part.effectivePath);
				ordered.Add(Label(part.effectivePath));
			}

			if (parts.Count == 0)
			{
				return string.Empty;
			}

			List<string> sorted = new(parts.Count);

			foreach (string name in CompositeOrder)
			{
				if (parts.TryGetValue(name, out string label))
				{
					sorted.Add(label);
				}
			}

			return string.Join(PartSeparator, sorted.Count == parts.Count ? sorted : ordered);
		}

		private static string FaceButton(string control, GamepadStyle style)
		{
			bool playStation = style == GamepadStyle.PlayStation;

			return control switch
			{
				"buttonSouth" => playStation ? "Cross" : "A",
				"buttonEast" => playStation ? "Circle" : "B",
				"buttonNorth" => playStation ? "Triangle" : "Y",
				_ => null
			};
		}

		private static string GamepadLabel(string control, string path)
		{
			return control switch
			{
				"leftStick" => "Left stick",
				"rightStick" => "Right stick",
				"leftStickPress" => "Left stick click",
				"rightStickPress" => "Right stick click",
				"buttonSouth" => "Bottom face button",
				"buttonEast" => "Right face button",
				"buttonWest" => "Left face button",
				"buttonNorth" => "Top face button",
				"rightTrigger" => "Right trigger",
				"leftTrigger" => "Left trigger",
				"rightShoulder" => "Right bumper",
				"leftShoulder" => "Left bumper",
				"start" => "Start",
				"select" => "Select",
				"dpad/up" => "D-pad up",
				"dpad/down" => "D-pad down",
				"dpad/left" => "D-pad left",
				"dpad/right" => "D-pad right",
				_ => Readable(path)
			};
		}

		private static string MouseLabel(string control, string path)
		{
			return control switch
			{
				"delta" => "Mouse",
				"leftButton" => "Left Mouse",
				"rightButton" => "Right Mouse",
				"middleButton" => "Middle Mouse",
				_ => Readable(path)
			};
		}

		private static string Readable(string path)
		{
			return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
		}
	}
}
