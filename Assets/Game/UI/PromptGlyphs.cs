using System;
using UnityEngine;

namespace NightCourier.UI
{
	[CreateAssetMenu(menuName = "Night Courier/Prompt Glyphs")]
	public sealed class PromptGlyphs : ScriptableObject
	{
		private const string KeyboardPrefix = "<Keyboard>/";
		private const string MousePrefix = "<Mouse>/";
		private const string KeyboardGlyph = "keyboard_";
		private const string MouseGlyph = "mouse_";

		[SerializeField] private Sprite[] _keys = Array.Empty<Sprite>();
		[SerializeField] private Sprite _lock;

		public Sprite Lock => _lock;

		public Sprite FindKey(string bindingPath)
		{
			string glyph = GlyphName(bindingPath);

			if (glyph == null)
			{
				return null;
			}

			foreach (Sprite sprite in _keys)
			{
				if (sprite != null && sprite.name == glyph)
				{
					return sprite;
				}
			}

			return null;
		}

		public static string GlyphName(string bindingPath)
		{
			if (string.IsNullOrEmpty(bindingPath))
			{
				return null;
			}

			if (bindingPath.StartsWith(KeyboardPrefix, StringComparison.Ordinal))
			{
				return KeyboardGlyph + KeyboardKey(bindingPath.Substring(KeyboardPrefix.Length));
			}

			if (bindingPath.StartsWith(MousePrefix, StringComparison.Ordinal))
			{
				return MouseButton(bindingPath.Substring(MousePrefix.Length));
			}

			return null;
		}

		private static string KeyboardKey(string control)
		{
			return control switch
			{
				"leftShift" or "rightShift" => "shift",
				"leftCtrl" or "rightCtrl" => "ctrl",
				"leftAlt" or "rightAlt" => "alt",
				"upArrow" => "arrow_up",
				"downArrow" => "arrow_down",
				"leftArrow" => "arrow_left",
				"rightArrow" => "arrow_right",
				_ => control
			};
		}

		private static string MouseButton(string control)
		{
			return control switch
			{
				"leftButton" => MouseGlyph + "left",
				"rightButton" => MouseGlyph + "right",
				_ => null
			};
		}
	}
}
