using System;
using UnityEngine.InputSystem;

namespace NightCourier.Controls
{
	public sealed class PlayerInputActions
	{
		public const string GameplayMapName = "Gameplay";
		public const string UiMapName = "UI";

		public PlayerInputActions(InputActionAsset asset)
		{
			if (asset == null)
			{
				throw new ArgumentNullException(nameof(asset));
			}

			Gameplay = asset.FindActionMap(GameplayMapName, true);
			Ui = asset.FindActionMap(UiMapName, true);

			Move = Gameplay.FindAction("Move", true);
			Look = Gameplay.FindAction("Look", true);
			Interact = Gameplay.FindAction("Interact", true);
			Drop = Gameplay.FindAction("Drop", true);
			Sprint = Gameplay.FindAction("Sprint", true);
			Pause = Gameplay.FindAction("Pause", true);
			Cancel = Ui.FindAction("Cancel", true);
		}

		public InputActionMap Gameplay { get; }

		public InputActionMap Ui { get; }

		public InputAction Move { get; }

		public InputAction Look { get; }

		public InputAction Interact { get; }

		public InputAction Drop { get; }

		public InputAction Sprint { get; }

		public InputAction Pause { get; }

		public InputAction Cancel { get; }
	}
}
