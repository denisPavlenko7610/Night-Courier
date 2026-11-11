#if UNITY_INCLUDE_TESTS
using NightCourier.Controls;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCourier.Tests
{
	public sealed class ControlLabelsTests
	{
		private const string ActionsPath = "Assets/Game/Content/Input/NightCourier.inputactions";

		private InputActionAsset _asset;
		private PlayerInputActions _actions;

		[SetUp]
		public void SetUp()
		{
			_asset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath));
			_actions = new PlayerInputActions(_asset);
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_asset);
		}

		[TestCase("<Keyboard>/escape", "Esc")]
		[TestCase("<Keyboard>/e", "E")]
		[TestCase("<Keyboard>/leftShift", "Left Shift")]
		[TestCase("<Mouse>/leftButton", "Left Mouse")]
		[TestCase("<Mouse>/delta", "Mouse")]
		[TestCase("<Gamepad>/buttonSouth", "Bottom face button")]
		[TestCase("<Gamepad>/dpad/up", "D-pad up")]
		[TestCase("<Gamepad>/rightTrigger", "Right trigger")]
		[TestCase("", "")]
		public void Label_NamesTheControl(string path, string expected)
		{
			Assert.That(ControlLabels.Label(path), Is.EqualTo(expected));
		}

		[TestCase("<Gamepad>/buttonSouth", GamepadStyle.Xbox, "A")]
		[TestCase("<Gamepad>/buttonSouth", GamepadStyle.PlayStation, "Cross")]
		[TestCase("<Gamepad>/buttonEast", GamepadStyle.Xbox, "B")]
		[TestCase("<Gamepad>/buttonEast", GamepadStyle.PlayStation, "Circle")]
		[TestCase("<Gamepad>/buttonNorth", GamepadStyle.PlayStation, "Triangle")]
		[TestCase("<Gamepad>/rightTrigger", GamepadStyle.PlayStation, "Right trigger")]
		[TestCase("<Keyboard>/e", GamepadStyle.PlayStation, "E")]
		public void PromptLabel_UsesTheDeviceStyle(string path, GamepadStyle style, string expected)
		{
			Assert.That(ControlLabels.PromptLabel(path, style), Is.EqualTo(expected));
		}

		[Test]
		public void PromptLabel_FollowsTheActiveScheme()
		{
			Assert.That(ControlLabels.PromptLabel(_actions.Interact, ControlScheme.KeyboardMouse, GamepadStyle.Xbox), Is.EqualTo("E"));
			Assert.That(ControlLabels.PromptLabel(_actions.Interact, ControlScheme.Gamepad, GamepadStyle.Xbox), Is.EqualTo("A"));
			Assert.That(ControlLabels.PromptLabel(_actions.Interact, ControlScheme.Gamepad, GamepadStyle.PlayStation), Is.EqualTo("Cross"));
		}

		[Test]
		public void FindPath_SkipsBindingsOfTheOtherScheme()
		{
			Assert.That(ControlLabels.FindPath(_actions.Interact, ControlScheme.Gamepad), Is.EqualTo("<Gamepad>/buttonSouth"));
			Assert.That(ControlLabels.FindPath(_actions.Interact, ControlScheme.KeyboardMouse), Is.EqualTo("<Keyboard>/e"));
			Assert.That(ControlLabels.FindPath(_actions.Move, ControlScheme.KeyboardMouse), Is.Null);
		}

		[Test]
		public void Describe_ReadsCompositesAndSeveralActions()
		{
			Assert.That(ControlLabels.Describe(_actions.Move, ControlScheme.KeyboardMouse), Is.EqualTo("W A S D"));
			Assert.That(ControlLabels.Describe(_actions.Move, ControlScheme.Gamepad), Is.EqualTo("Left stick"));
			Assert.That(
				ControlLabels.Describe(ControlScheme.KeyboardMouse, _actions.SwitchWeapon, _actions.Weapon1, _actions.Weapon2),
				Is.EqualTo("Q / 1 / 2"));
			Assert.That(
				ControlLabels.Describe(ControlScheme.Gamepad, _actions.SwitchWeapon, _actions.Weapon1, _actions.Weapon2),
				Is.EqualTo("Top face button / D-pad left / D-pad right"));
		}

		[Test]
		public void Describe_FollowsRebinding()
		{
			_actions.Interact.ApplyBindingOverride(0, "<Keyboard>/f");

			Assert.That(ControlLabels.Describe(_actions.Interact, ControlScheme.KeyboardMouse), Is.EqualTo("F"));
			Assert.That(ControlLabels.PromptLabel(_actions.Interact, ControlScheme.KeyboardMouse, GamepadStyle.Xbox), Is.EqualTo("F"));
		}
	}
}
#endif
