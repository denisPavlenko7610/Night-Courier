using System;
using NightCourier.Controls;
using RD.Localization;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace NightCourier.UI
{
	public static class ControlsTable
	{
		public const string KeyboardRowsName = "controls-keyboard-rows";
		public const string GamepadRowsName = "controls-gamepad-rows";

		private const string RowClass = "nc-controls__row";
		private const string KeysClass = "nc-controls__keys";
		private const string ActionClass = "nc-controls__actions";

		public static void Populate(VisualElement root, PlayerInputActions actions, ILocalizationService localization)
		{
			if (root == null)
			{
				throw new ArgumentNullException(nameof(root));
			}

			if (actions == null)
			{
				throw new ArgumentNullException(nameof(actions));
			}

			if (localization == null)
			{
				throw new ArgumentNullException(nameof(localization));
			}

			Fill(UiQuery.Require<VisualElement>(root, KeyboardRowsName), actions, localization, ControlScheme.KeyboardMouse);
			Fill(UiQuery.Require<VisualElement>(root, GamepadRowsName), actions, localization, ControlScheme.Gamepad);
		}

		private static void Fill(VisualElement container, PlayerInputActions actions, ILocalizationService localization, ControlScheme scheme)
		{
			container.Clear();

			Add(container, localization, scheme, "controls.action.move", actions.Move);
			Add(container, localization, scheme, "controls.action.look", actions.Look);
			Add(container, localization, scheme, "controls.action.interact", actions.Interact);
			Add(container, localization, scheme, "controls.action.drop", actions.Drop);
			Add(container, localization, scheme, "controls.action.sprint", actions.Sprint);
			Add(container, localization, scheme, "controls.action.fire", actions.Fire);
			Add(container, localization, scheme, "controls.action.reload", actions.Reload);
			Add(container, localization, scheme, "controls.action.medkit", actions.UseMedkit);
			Add(container, localization, scheme, "controls.action.weapon", actions.SwitchWeapon);
			Add(container, localization, scheme, "controls.action.select", actions.Weapon1, actions.Weapon2);
			Add(container, localization, scheme, "controls.action.pause", actions.Pause);
		}

		private static void Add(
			VisualElement container,
			ILocalizationService localization,
			ControlScheme scheme,
			string actionKey,
			params InputAction[] actions)
		{
			string keys = ControlLabels.Describe(scheme, actions);

			if (keys.Length == 0)
			{
				return;
			}

			VisualElement row = new();
			Label keysLabel = new(keys);
			Label actionLabel = new(localization.Get(actionKey));

			row.AddToClassList(RowClass);
			keysLabel.AddToClassList(KeysClass);
			actionLabel.AddToClassList(ActionClass);
			row.Add(keysLabel);
			row.Add(actionLabel);
			container.Add(row);
		}
	}
}
